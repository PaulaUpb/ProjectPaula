using System;
using System.Collections.Generic;

namespace ProjectPaula.Model.ObjectSynchronization
{
    /// <summary>
    /// Manages the <see cref="ObjectSynchronizationContext"/> instances.
    /// </summary>
    public class ObjectSynchronizationManager
    {
        public static ObjectSynchronizationManager Current { get; } = new ObjectSynchronizationManager();

        private readonly object _lock = new object();

        // For each hub derived from ObjectSynchronizationHub<T> this holds a manager for object synchronization
        // (because hub instances come and go and we need something persistent)
        private Dictionary<Type, ObjectSynchronizationContext> _syncManagers = new Dictionary<Type, ObjectSynchronizationContext>();

        public ObjectSynchronizationContext GetContext<T>(ObjectSynchronizationHub<T> hub) where T : class, IObjectSynchronizationHubClient
        {
            lock (_lock)
            {
                ObjectSynchronizationContext syncManager;

                if (_syncManagers.TryGetValue(hub.GetType(), out syncManager))
                {
                    return syncManager;
                }
                else
                {
                    syncManager = new ObjectSynchronizationContext(hub);
                    _syncManagers.Add(hub.GetType(), syncManager);
                    return syncManager;
                }
            }
        }
    }
}
