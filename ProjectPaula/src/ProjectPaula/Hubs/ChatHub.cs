using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectPaula.Hubs
{
    public class ChatManager
    {
        public static Dictionary<string, string> Users { get; } = new Dictionary<string, string>();
    }

    public class ChatHub : Hub<IChatHubClient>
    {
        public void Send(string message)
        {
            Clients.All.AddMessage(ChatManager.Users[Context.ConnectionId], message);
        }

        public void Register(string name)
        {
            Clients.Others.AddSystemMessage($"A new client connected: {name}");
            Clients.Caller.AddSystemMessage($"You have connected as \"{name}\"");
            ChatManager.Users.Add(Context.ConnectionId, name);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Clients.All.AddSystemMessage("A client disconnected: " + ChatManager.Users[Context.ConnectionId]);
            ChatManager.Users.Remove(Context.ConnectionId);
            return Task.FromResult(0);
        }
    }

    public interface IChatHubClient
    {
        void AddMessage(string name, string message);

        void AddSystemMessage(string message);
    }
}
