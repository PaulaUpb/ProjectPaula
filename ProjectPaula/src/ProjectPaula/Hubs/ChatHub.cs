using ProjectPaula.Model.ObjectSynchronization;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Hubs
{
    public class ChatViewModel : BindableBase
    {
        private static Lazy<ChatViewModel> _instance = new Lazy<ChatViewModel>(() => new ChatViewModel());
        public static ChatViewModel Instance => _instance.Value;

        public ObservableCollection<ChatUserViewModel> Users { get; } = new ObservableCollection<ChatUserViewModel>();

        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

        private ChatViewModel()
        {
            Messages.Add(new ChatMessage("SYSTEM", "Hello synchronized SignalR world!"));
        }

        public ChatUserViewModel GetUser(string connectionId)
            => Users.FirstOrDefault(o => o.ConnectionId == connectionId);
    }

    public class ChatUserViewModel : BindableBase
    {
        public string ConnectionId { get; }
        public string Name { get; }

        public ChatUserViewModel(string connectionId, string name)
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
        private static object _lock = new object();

        public void Send(string message)
        {
            var chatVM = CallerSynchronizedObjects["Chat"] as ChatViewModel;

            lock (_lock)
            {
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
        }

        /// <summary>
        /// This is called once by each client to login to the chat.
        /// </summary>
        /// <param name="name">Username</param>
        public void Register(string name)
        {
            CallerSynchronizedObjects.Add("Chat", ChatViewModel.Instance);
            ChatViewModel.Instance.Users.Add(new ChatUserViewModel(Context.ConnectionId, name));
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // We do not need to call CallerSynchronizedObjects.Remove("Chat");
            // because this is done automatically as the client disconnects.

            ChatViewModel.Instance.Users.Remove(ChatViewModel.Instance.GetUser(Context.ConnectionId));
            await base.OnDisconnectedAsync(exception);
        }
    }

    public interface IChatHubClient : IObjectSynchronizationHubClient
    {
    }
}
