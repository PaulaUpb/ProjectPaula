using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace ProjectPaula.Model.ObjectSynchronization
{
    /// <summary>
    /// Base type for SignalR hubs that use the object synchronization feature.
    /// </summary>
    /// <typeparam name="T">Client interface type</typeparam>
    public abstract class ObjectSynchronizationHub<T> : Hub<T> where T : class, IObjectSynchronizationHubClient
    {
        private ObjectSynchronizationContext _syncManager;

        /// <summary>
        /// Provides methods to enable or disable synchronization of arbitrary objects.
        /// Use this to add or remove objects to/from specific clients.
        /// </summary>
        public ObjectSynchronizationContext SynchronizedObjects
        {
            get
            {
                if (_syncManager == null)
                    _syncManager = ObjectSynchronizationManager.Current.GetObjectSynchronizationContext(this);
                return _syncManager;
            }
        }

        /// <summary>
        /// Returns an <see cref="ObjectSynchronizationClient"/> for the
        /// current client connection. Use this to add or remove objects
        /// to/from the calling client.
        /// </summary>
        public ObjectSynchronizationClient CallerSynchronizedObjects
            => SynchronizedObjects[Context.ConnectionId];

        /// <summary>
        /// Overridden methods should call this base implementation
        /// BEFORE dealing with synchronized objects.
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnected()
        {
            await base.OnConnected();
            SynchronizedObjects.AddClient(Context.ConnectionId);
        }

        /// <summary>
        /// Overridden methods should call this base implementation
        /// AFTER dealing with synchronized objects.
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override async Task OnDisconnected(bool stopCalled)
        {
            SynchronizedObjects.RemoveClient(Context.ConnectionId);
            await base.OnDisconnected(stopCalled);
        }
    }
}
