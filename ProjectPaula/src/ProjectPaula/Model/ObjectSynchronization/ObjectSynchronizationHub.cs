using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

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
                    _syncManager = ObjectSynchronizationManager.Current.GetContext(this);
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
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            SynchronizedObjects.AddClient(Context.ConnectionId);
        }

        /// <summary>
        /// Overridden methods should call this base implementation
        /// AFTER dealing with synchronized objects.
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            SynchronizedObjects.RemoveClient(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
