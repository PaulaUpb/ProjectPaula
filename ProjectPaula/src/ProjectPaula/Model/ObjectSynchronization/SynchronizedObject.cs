using Microsoft.AspNet.SignalR;
using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ProjectPaula.Model.ObjectSynchronization
{
    /// <summary>
    /// Synchronizes an object with SignalR clients.
    /// Changes to the object are detected through an <see cref="ObjectTracker"/>
    /// and then forwarded to the clients with which the object is synchronized.
    /// </summary>
    public class SynchronizedObject : IDisposable
    {
        private readonly Hub _syncHub; // Unfortunately, using a typed hub doesn't work out here (because generics)
        private readonly List<ConnectionToken> _connections = new List<ConnectionToken>();
        private readonly ObjectTracker _tracker;
        private readonly object _lock = new object();

        /// <summary>
        /// The object that is being synchronized.
        /// </summary>
        public object Object => _tracker.TrackedObject;

        /// <summary>
        /// Gets a collection of connection IDs that refer to
        /// the clients that the object is synchronized with.
        /// </summary>
        public IEnumerable<string> ConnectedClients => _connections.Select(o => o.Id);

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
        public SynchronizedObject(Hub syncHub, object obj)
        {
            if (syncHub == null)
                throw new ArgumentNullException(nameof(syncHub));

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

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
        /// <param name="key">The key that is used to access the object</param>
        internal void AddConnection(string connectionId, string key)
        {
            lock (_lock)
            {
                if (_connections.Any(t => t.Id == connectionId))
                    return;

                var token = new ConnectionToken(connectionId, key);
                _connections.Add(token);
            }

            // Push the object to the new client
            _syncHub.Clients.Client(connectionId).InitializeObject(key, Object);
        }

        /// <summary>
        /// Disables synchronization of the object with the
        /// client specified by <paramref name="connectionId"/>.
        /// After this the client can no longer access the object.
        /// </summary>
        /// <param name="connectionId">Connection ID</param>
        /// <returns>True if such a connection existed and has been removed</returns>
        internal bool RemoveConnection(string connectionId)
        {
            lock (_lock)
            {
                var token = _connections.SingleOrDefault(o => o.Id == connectionId);
                return (token != null) && RemoveConnection(token);
            }
        }

        private bool RemoveConnection(ConnectionToken connection)
        {
            _syncHub.Clients.Client(connection.Id).RemoveObject(connection.ObjectKey);
            return _connections.Remove(connection);
        }

        private void OnTrackerPropertyChanged(ObjectTracker sender, PropertyPathChangedEventArgs e)
        {
            var isCollection = e.Object.GetType().GetInterfaces().Any(o =>
                o.GetTypeInfo().IsGenericType &&
                o.GetGenericTypeDefinition() == typeof(ICollection<>));

            // The "Count" property of collections is redundant, so we do not sync its changes
            // (remember we still have the "length" property on the JavaScript side)
            if (isCollection && e.PropertyName == "Count")
                return;

            lock (_lock)
            {
                foreach (var connection in _connections)
                {
                    _syncHub.Clients.Client(connection.Id).PropertyChanged(connection.ObjectKey, e);
                }
            }
        }

        private void OnTrackerCollectionChanged(ObjectTracker sender, CollectionPathChangedEventArgs e)
        {
            lock (_lock)
            {
                foreach (var connection in _connections)
                    _syncHub.Clients.Client(connection.Id).CollectionChanged(connection.ObjectKey, e);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                // Remove object from remaining clients
                while (_connections.Any())
                    RemoveConnection(_connections[0]);
            }

            // Dispose object tracker
            _tracker.PropertyChanged -= OnTrackerPropertyChanged;
            _tracker.CollectionChanged -= OnTrackerCollectionChanged;
            _tracker.Dispose();
        }

        class ConnectionToken
        {
            /// <summary>
            /// The connection ID of the SignalR client.
            /// </summary>
            public string Id { get; }

            /// <summary>
            /// The key that is used to access the synchronized object.
            /// </summary>
            public string ObjectKey { get; }

            public ConnectionToken(string connectionId, string key)
            {
                Id = connectionId;
                ObjectKey = key;
            }
        }
    }
}
