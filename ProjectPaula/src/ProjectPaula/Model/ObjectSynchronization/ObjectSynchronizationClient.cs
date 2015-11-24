using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.Model.ObjectSynchronization
{
    public class ObjectSynchronizationClient : IDictionary<string, object>, IDisposable
    {
        private Dictionary<string, SynchronizedObject> _syncedObjects = new Dictionary<string, SynchronizedObject>();
        private ObjectSynchronizationContext _context; // Used to obtain SynchronizedObjects for objects
        private string _connectionId;

        public ICollection<string> Keys => _syncedObjects.Keys;

        public ICollection<object> Values => _syncedObjects.Values.Select(o => o.Object).ToList();

        public int Count => _syncedObjects.Count;


        public ObjectSynchronizationClient(ObjectSynchronizationContext context, string connectionId)
        {
            _context = context;
            _connectionId = connectionId;
        }

        /// <summary>
        /// Gets a synchronized object by its key or
        /// begins/ends synchronization of objects.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get
            {
                SynchronizedObject o;
                if (_syncedObjects.TryGetValue(key, out o))
                    return o.Object;
                else
                    throw new KeyNotFoundException();
            }
            set
            {
                SynchronizedObject o;

                if (_syncedObjects.TryGetValue(key, out o))
                {
                    // Remove old object
                    Remove(key);
                }

                if (value != null)
                {
                    // Add new object
                    Add(key, value);
                }
            }
        }

        public bool ContainsKey(string key)
            => _syncedObjects.ContainsKey(key);

        /// <summary>
        /// Adds the specified object to the list of synchronized
        /// objects. This enables synchronization of the object with
        /// the client.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Object</param>
        public void Add(string key, object value)
        {
            if (ContainsKey(key))
                throw new ArgumentException($"The key '{key}' is already in use");

            var o = _context.GetSynchronizedObject(value);
            o.AddConnection(_connectionId, key);
            _syncedObjects.Add(key, o);
        }

        /// <summary>
        /// Removes the object with the specified key from the list of
        /// synchronized objects. This disables synchronization of the
        /// object with the client and deletes the object on the client.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if removal has been successful</returns>
        public bool Remove(string key)
        {
            SynchronizedObject o;

            if (_syncedObjects.TryGetValue(key, out o))
            {
                o.RemoveConnection(_connectionId);
                _syncedObjects.Remove(key);
                _context.CleanUpSynchronizedObject(o);
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out object value)
        {
            SynchronizedObject o;

            if (_syncedObjects.TryGetValue(key, out o))
            {
                value = o.Object;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            SynchronizedObject o;
            if (_syncedObjects.TryGetValue(key, out o))
            {
                value = (T)o.Object;
                return true;
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Removes all synchronized objects from the client.
        /// </summary>
        public void Clear()
        {
            foreach (var kvp in _syncedObjects)
            {
                kvp.Value.RemoveConnection(_connectionId);
                _context.CleanUpSynchronizedObject(kvp.Value);
            }

            _syncedObjects.Clear();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            => _syncedObjects.Select(o => new KeyValuePair<string, object>(o.Key, o.Value.Object)).GetEnumerator();

        public void Dispose()
        {
            // Remove all synchronized objects
            Clear();
        }

        #region Explicit ICollection<> implementations
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
            => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            SynchronizedObject o;
            return _syncedObjects.TryGetValue(item.Key, out o) && Equals(o.Object, item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _syncedObjects
                .Select(o => new KeyValuePair<string, object>(o.Key, o.Value.Object))
                .ToList()
                .CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
            => ((ICollection<KeyValuePair<string, object>>)this).Contains(item) && Remove(item.Key);

        #endregion
    }
}
