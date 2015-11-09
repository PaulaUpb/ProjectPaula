using Microsoft.AspNet.SignalR;

namespace ProjectPaula.Model.ObjectSynchronization
{
    /// <summary>
    /// Base type for SignalR hubs that use the object synchronization feature.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObjectSynchronizationHub<T> : Hub<T> where T : class, IObjectSynchronizationClient
    {
        private ObjectSynchronizationContext _syncManager;

        /// <summary>
        /// Provides methods to enable synchronization of arbitrary objects
        /// with specific clients.
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
    }
}
