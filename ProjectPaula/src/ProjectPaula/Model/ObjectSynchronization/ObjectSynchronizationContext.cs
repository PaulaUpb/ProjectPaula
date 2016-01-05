using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProjectPaula.Model.ObjectSynchronization
{
    /// <summary>
    /// The hub-specific context for object synchronization.
    /// Every hub type that derives from <see cref="ObjectSynchronizationHub{T}"/> is
    /// assigned exactly one instance of <see cref="ObjectSynchronizationContext"/>.
    /// Because hub instances are transient the context objects are managed by
    /// <see cref="ObjectSynchronizationManager"/>.
    /// </summary>
    public class ObjectSynchronizationContext
    {
        private Hub _syncHub;
        private readonly object _lock = new object();

        // Maps connection IDs to ObjectSynchronizationClients
        private Dictionary<string, ObjectSynchronizationClient> _clients =
            new Dictionary<string, ObjectSynchronizationClient>();

        private ConditionalWeakTable<object, SynchronizedObject> _syncedObjects =
            new ConditionalWeakTable<object, SynchronizedObject>();

        /// <summary>
        /// Initializes a new <see cref="ObjectSynchronizationContext"/>
        /// for the specified hub.
        /// </summary>
        /// <param name="syncHub">A hub that derives from <see cref="ObjectSynchronizationHub{T}"/></param>
        public ObjectSynchronizationContext(Hub syncHub)
        {
            if (syncHub == null)
                throw new ArgumentNullException(nameof(syncHub));

            _syncHub = syncHub;
        }

        /// <summary>
        /// Returns an <see cref="ObjectSynchronizationClient"/>
        /// for the client with the specified connection ID.
        /// </summary>
        /// <param name="connectionId">Connection ID</param>
        /// <returns><see cref="ObjectSynchronizationClient"/></returns>
        public ObjectSynchronizationClient this[string connectionId]
        {
            get
            {
                lock (_lock)
                {
                    return _clients[connectionId];
                }
            }
        }
        
        internal ObjectSynchronizationClient AddClient(string connectionId)
        {
            lock (_lock)
            {
                ObjectSynchronizationClient client;

                if (_clients.TryGetValue(connectionId, out client))
                {
                    // Return existing client
                    return client;
                }
                else
                {
                    // Create new client for specified connection ID
                    client = new ObjectSynchronizationClient(this, connectionId);
                    _clients.Add(connectionId, client);
                    return client;
                }
            }
        }

        internal bool RemoveClient(string connectionId)
        {
            lock (_lock)
            {
                ObjectSynchronizationClient client;

                if (_clients.TryGetValue(connectionId, out client))
                {
                    client.Dispose();
                    return _clients.Remove(connectionId);
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the single <see cref="SynchronizedObject"/> instance
        /// for the hub that is used to synchronize the specified object.
        /// </summary>
        /// <remarks>
        /// If an object is synchronized with multiple clients we only
        /// want to maintain one <see cref="SynchronizedObject"/> instance
        /// (and only one <see cref="ObjectTracker"/>) for that object.
        /// </remarks>
        /// <param name="o">Object</param>
        /// <returns></returns>
        internal SynchronizedObject GetSynchronizedObject(object o)
        {
            lock (_lock)
            {
                SynchronizedObject syncedObject;

                if (_syncedObjects.TryGetValue(o, out syncedObject))
                {
                    // Return existing SynchronizedObject
                    return syncedObject;
                }
                else
                {
                    // Create new SynchronizedObject
                    syncedObject = new SynchronizedObject(_syncHub, o);
                    _syncedObjects.Add(o, syncedObject);
                    return syncedObject;
                }
            }
        }

        /// <summary>
        /// Checks if the <see cref="SynchronizedObject"/> is still in use
        /// by at least one client. If this is not the case it is disposed
        /// so that the associated object is no longer tracked.
        /// </summary>
        /// <param name="o">Synchronized object</param>
        internal void CleanUpSynchronizedObject(SynchronizedObject o)
        {
            lock (_lock)
            {
                if (!o.ConnectedClients.Any())
                {
                    o.Dispose();
                    _syncedObjects.Remove(o.Object);
                }
            }
        }
    }
}
