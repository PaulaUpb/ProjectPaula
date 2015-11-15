using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    /// <summary>
    /// The base class for classes that observe specific aspects of an object.
    /// <see cref="PropertyChangeObserver"/> extends this to listen for
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> events.
    /// <see cref="CollectionChangeObserver"/> extends this to listen for
    /// <see cref="INotifyCollectionChanged.CollectionChanged"/> events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class ObjectObserverBase<T> : IObjectObserver where T : class
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected ObjectTracker Tracker { get; private set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T TrackedObject => (T)Tracker.TrackedObject;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object IObjectObserver.TrackedObject => Tracker.TrackedObject;

        public void Initialize(ObjectTracker tracker)
        {
            if (Tracker != null)
                throw new InvalidOperationException("Already initialized");

            Tracker = tracker;
            Attach();
        }

        protected virtual void Attach() { }

        protected virtual void Detach() { }

        public void Dispose() => Detach();
    }
}
