using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;

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
        private Dictionary<string, SynchronizedObject> _syncedObjects = new Dictionary<string, SynchronizedObject>();

        /// <summary>
        /// Initializes a new <see cref="ObjectSynchronizationHub{T}"/>
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
        /// Adds an object to the list of synchronized objects.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="obj">Object</param>
        /// <returns>
        /// A <see cref="SynchronizedObject"/> that can be used to add
        /// connections with which the object should be synchronized.
        /// </returns>
        public SynchronizedObject Add(string key, object obj)
        {
            if (_syncedObjects.ContainsKey(key))
                throw new InvalidOperationException($"The key '{key}' is already in use");

            var syncedObject = new SynchronizedObject(_syncHub, key, obj);
            _syncedObjects.Add(key, syncedObject);
            return syncedObject;
        }

        /// <summary>
        /// Removes the object with the specified key from the list
        /// of synchronized objects. The object will be removed from
        /// all clients with which it was synchronized.
        /// </summary>
        /// <param name="key">Key</param>
        public void Remove(string key)
        {
            SynchronizedObject syncedObject;

            if (_syncedObjects.TryGetValue(key, out syncedObject))
            {
                syncedObject.Dispose();
                _syncedObjects.Remove(key);
            }
        }

        /// <summary>
        /// Returns the synchronized object with the specified key.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns><see cref="SynchronizedObject"/> or null if none exists for the key</returns>
        public SynchronizedObject this[string key]
        {
            get
            {
                SynchronizedObject o;
                return _syncedObjects.TryGetValue(key, out o) ? o : null;
            }
        }
    }
}
