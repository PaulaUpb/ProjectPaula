using System;
using System.Diagnostics;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    /// <summary>
    /// Listens to events of an <see cref="ObjectTracker"/> and forwards
    /// them to the parent <see cref="ObjectTracker"/>.
    /// </summary>
    /// <example>
    /// Example:
    /// - From an <see cref="ObjectTracker"/> which tracks an Employee we get
    ///   notified that property "Name" has changed
    /// - That event is forwarded to the parent <see cref="ObjectTracker"/> (which
    ///   tracks a collection of Employees) with property path "[4].Name"
    /// - That event is forwarded to the parent <see cref="ObjectTracker"/> (which
    ///   tracks a Company object) with property path "Employees[4].Name"
    /// </example>
    class TrackerToken : IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _pathPrefix;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObjectTracker _parentTracker;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ObjectTracker Tracker { get; set; }

        /// <summary>
        /// Creates a new <see cref="ObjectTracker"/> for the specified object
        /// and subscribes to its events in order to propagate events to the
        /// specified parent <see cref="ObjectTracker"/>.
        /// </summary>
        /// <param name="o">Object to be tracked</param>
        /// <param name="pathPrefix">Property path prefix</param>
        /// <param name="parentTracker">Parent <see cref="ObjectTracker"/></param>
        public TrackerToken(object o, string pathPrefix, ObjectTracker parentTracker)
        {
            _pathPrefix = pathPrefix;
            _parentTracker = parentTracker;
            Tracker = new ObjectTracker(o);
            Tracker.PropertyChanged += OnTrackerPropertyChanged;
            Tracker.CollectionChanged += OnTrackerCollectionChanged;
        }

        private void OnTrackerPropertyChanged(object sender, PropertyPathChangedEventArgs e)
        {
            e.PropertyPath = ConstructPropertyPathForParent(e.PropertyPath);
            _parentTracker.RaisePropertyPathChanged(e);
        }

        private void OnTrackerCollectionChanged(object sender, CollectionPathChangedEventArgs e)
        {
            e.PropertyPath = ConstructPropertyPathForParent(e.PropertyPath);
            _parentTracker.RaiseCollectionPathChanged(e);
        }

        private string ConstructPropertyPathForParent(string path)
        {
            if (string.IsNullOrEmpty(path))
                return _pathPrefix;
            else
                return $"{_pathPrefix}.{path}";
        }

        /// <summary>
        /// Unsubscribes from the events of the <see cref="ObjectTracker"/>
        /// and disposes the <see cref="ObjectTracker"/>.
        /// </summary>
        public void Dispose()
        {
            Tracker.PropertyChanged -= OnTrackerPropertyChanged;
            Tracker.CollectionChanged -= OnTrackerCollectionChanged;
            Tracker.Dispose();
        }

        public override string ToString()
            => $"Tracker for '{_pathPrefix}' of type '{Tracker.TrackedObject.GetType().Name}'";
    }
}
