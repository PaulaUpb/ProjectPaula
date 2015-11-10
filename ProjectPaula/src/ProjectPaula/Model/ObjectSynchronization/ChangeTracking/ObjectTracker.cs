using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    /// <summary>
    /// Tracks property changes and collection changes on an arbitrary object and
    /// all of its descendant objects. The events raised by the <see cref="ObjectTracker"/>
    /// always include the full path to the property/collection that changed.
    /// </summary>
    /// <remarks>
    /// Things to keep in mind:
    /// (1) Only public instance properties are tracked
    /// (2) Types must implement <see cref="INotifyPropertyChanged"/>
    ///     for property change events
    /// (3) Types must implement <see cref="INotifyCollectionChanged"/>
    ///     for collection change events
    /// (4) The public properties within an object graph must not create circular references
    /// (5) The properties serialized by a JSON serializer should match the
    ///     properties that are tracked by the <see cref="ObjectTracker"/>.
    ///     (TODO: This is currently not the case for properties on collections because
    ///     collections are JSON-serialized as arrays, but arrays cannot contain further
    ///     properties. However these properties are pushed to the JS-arrays on the clients
    ///     as soon as a PropertyChanged event is triggered for them)
    /// </remarks>
    public class ObjectTracker : INotifyObjectChanged, IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object _trackedObject;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private readonly IObjectObserver[] _observers;

        public event PropertyPathChangedEventHandler PropertyChanged;

        public event CollectionPathChangedEventHandler CollectionChanged;

        /// <summary>
        /// The object that is observed for changes.
        /// </summary>
        public object TrackedObject => _trackedObject;

        /// <summary>
        /// Initializes a new <see cref="ObjectTracker"/> to track changes
        /// on the specified object. Only public instance properties are
        /// tracked and the specified object must not contain circular
        /// references.
        /// </summary>
        /// <param name="trackedObject">The object to be observed</param>
        public ObjectTracker(object trackedObject)
        {
            _trackedObject = trackedObject;
            _observers = CreateObservers().ToArray();
        }

        internal void RaisePropertyPathChanged(PropertyPathChangedEventArgs e)
            => PropertyChanged?.Invoke(this, e);

        internal void RaiseCollectionPathChanged(CollectionPathChangedEventArgs e)
            => CollectionChanged?.Invoke(this, e);

        private IEnumerable<IObjectObserver> CreateObservers()
        {
            // TODO: We should find applicable observer types via reflection

            if (_trackedObject is INotifyPropertyChanged)
            {
                var observer = new NotifyPropertyChangedObserver();
                observer.Initialize(this);
                yield return observer;
            }

            if (_trackedObject is INotifyCollectionChanged)
            {
                var observer = new NotifyCollectionChangedObserver();
                observer.Initialize(this);
                yield return observer;
            }
        }

        public void Dispose()
        {
            foreach (var observer in _observers)
                observer.Dispose();
        }
    }
}
