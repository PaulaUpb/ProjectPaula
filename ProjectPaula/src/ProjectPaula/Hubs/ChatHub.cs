using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Hubs
{
    public class ChatViewModel : BindableBase
    {
        public ObservableCollection<UserViewModel> Users { get; } = new ObservableCollection<UserViewModel>();

        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

        public ChatViewModel()
        {
            Messages.Add(new ChatMessage("SYSTEM", "Hello synchronized SignalR world!"));
        }

        public UserViewModel GetUser(string connectionId)
            => Users.FirstOrDefault(o => o.ConnectionId == connectionId);
    }

    public class UserViewModel : BindableBase
    {
        public string ConnectionId { get; }
        public string Name { get; }

        public UserViewModel(string connectionId, string name)
        {
            ConnectionId = connectionId;
            Name = name;
        }
    }

    public class ChatMessage
    {
        public string User { get; }
        public string Message { get; }

        public ChatMessage(string user, string message)
        {
            User = user;
            Message = message;
        }
    }

    public class ChatHub : ObjectSynchronizationHub<IChatHubClient>
    {
        public void Send(string message)
        {
            var chatVM = SynchronizedObjects["Chat"].Object as ChatViewModel;

            if (message == "Reset")
            {
                chatVM.Messages.Clear();
            }
            else if (message == "Remove")
            {
                chatVM.Messages.RemoveAt(0);
            }
            else if (message == "Insert")
            {
                chatVM.Messages.Insert(2, new ChatMessage("SYSTEM", "An message inserted at index 2 here!"));
            }
            else
            {
                var userName = chatVM.GetUser(Context.ConnectionId)?.Name;
                chatVM.Messages.Add(new ChatMessage(userName, message));
            }
        }

        /// <summary>
        /// This is called once by each client to login to the chat.
        /// </summary>
        /// <param name="name">Username</param>
        public void Register(string name)
        {
            var chat = SynchronizedObjects["Chat"];

            if (chat == null)
            {
                var chatVM = new ChatViewModel();
                chat = SynchronizedObjects.Add("Chat", chatVM);
            }

            (chat.Object as ChatViewModel).Users.Add(new UserViewModel(Context.ConnectionId, name));
            chat.AddConnection(Context.ConnectionId);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var chatVM = SynchronizedObjects["Chat"].Object as ChatViewModel;
            chatVM.Users.Remove(chatVM.GetUser(Context.ConnectionId));

            SynchronizedObjects["Chat"].RemoveConnection(Context.ConnectionId);

            return Task.FromResult(0);
        }
    }

    public interface IChatHubClient : IObjectSynchronizationClient
    {
    }
}
