using Microsoft.AspNet.SignalR;
using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.Model.ObjectSynchronization
{
    /// <summary>
    /// Synchronizes an object with SignalR clients.
    /// Changes to the object are detected through an <see cref="ObjectTracker"/>
    /// and then forwarded to the clients with which the object is synchronized.
    /// </summary>
    public class SynchronizedObject : IDisposable
    {
        private Hub _syncHub;
        private List<string> _connectionIds = new List<string>();
        private ObjectTracker _tracker;
        private string _key;

        public object Object => _tracker.TrackedObject;

        /// <summary>
        /// Initializes a new <see cref="SynchronizedObject"/>.
        /// </summary>
        /// <param name="syncHub">
        /// The hub (derived from <see cref="ObjectSynchronizationHub{T}"/>) that
        /// is used to forwards PropertyChanged and CollectionChanged events to clients
        /// </param>
        /// <param name="key">
        /// The key that identifies the object within an <see cref="ObjectSynchronizationContext"/>.
        /// The key is also used on the client side to access the object via JS.
        /// </param>
        /// <param name="obj">The object to be synchronized</param>
        public SynchronizedObject(Hub syncHub, string key, object obj)
        {
            if (syncHub == null)
                throw new ArgumentNullException(nameof(syncHub));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException(nameof(key));

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            _key = key;
            _syncHub = syncHub;
            _tracker = new ObjectTracker(obj);
            _tracker.PropertyChanged += OnTrackerPropertyChanged;
            _tracker.CollectionChanged += OnTrackerCollectionChanged;
        }

        /// <summary>
        /// Enables synchronization of the object with the
        /// client specified by <paramref name="connectionId"/>.
        /// </summary>
        /// <param name="connectionId">Connection ID</param>
        public void AddConnection(string connectionId)
        {
            if (_connectionIds.Contains(connectionId))
                return;

            _connectionIds.Add(connectionId);

            // Push the object to the new client
            _syncHub.Clients.Client(connectionId).InitializeObject(_key, Object);
        }

        /// <summary>
        /// Disables synchronization of the object with the
        /// client specified by <paramref name="connectionId"/>.
        /// After this the client can no longer access the object.
        /// </summary>
        /// <param name="connectionId">Connection ID</param>
        public void RemoveConnection(string connectionId)
        {
            if (_connectionIds.Remove(connectionId))
            {
                // TODO: Remove object from client
                //_syncHub.Clients.Client(connectionId).RemoveObject(Object);
            }
        }

        private void OnTrackerPropertyChanged(ObjectTracker sender, PropertyPathChangedEventArgs e)
        {
            _syncHub.Clients.Clients(_connectionIds).PropertyChanged(_key, e);
        }

        private void OnTrackerCollectionChanged(ObjectTracker sender, CollectionPathChangedEventArgs e)
        {
            _syncHub.Clients.Clients(_connectionIds).CollectionChanged(_key, e);
        }

        public void Dispose()
        {
            // Remove object from remaining clients
            while (_connectionIds.Any())
                RemoveConnection(_connectionIds[0]);

            // Dispose object tracker
            _tracker.PropertyChanged -= OnTrackerPropertyChanged;
            _tracker.CollectionChanged -= OnTrackerCollectionChanged;
            _tracker.Dispose();
        }
    }
}
