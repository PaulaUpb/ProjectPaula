using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    /// <summary>
    /// Subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
    /// of an object to determine property changes.
    /// 
    /// TODO: PropertyChanged(string.Empty) is not yet handled properly.
    /// </summary>
    class PropertyChangeObserver : ObjectObserverBase<INotifyPropertyChanged>
    {
        // This is for the "children" properties of the observed target
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private Dictionary<string, TrackerToken> _propertyTrackers =
            new Dictionary<string, TrackerToken>();

        protected override void Attach()
        {
            TrackedObject.PropertyChanged += OnPropertyChanged;

            // Scan through the properties to see if we need to
            // observe nested objects; for each property add an
            // ObjectTracker.
            var properties = TrackedObject.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetIndexParameters().Length == 0)
                .Where(prop => !prop.CustomAttributes.Any(o => o.AttributeType == typeof(DoNotTrackAttribute)))
                .ToArray();

            foreach (var prop in properties)
                RefreshPropertyTracker(prop.Name, raiseEvent: false); // Do not raise events on initialization
        }

        protected override void Detach()
        {
            TrackedObject.PropertyChanged -= OnPropertyChanged;
        }

        private void RefreshPropertyTracker(string propertyName, bool raiseEvent)
        {
            // Determine the new value
            var prop = TrackedObject.GetType().GetProperty(propertyName);
            if (prop == null)
                return; // Was probably an indexer that changed
            var newValue = prop.GetValue(TrackedObject);

            // If no ObjectTracker exists for the property, the old value was null
            TrackerToken trackerToken;
            object oldValue = null;

            if (_propertyTrackers.TryGetValue(propertyName, out trackerToken))
            {
                // Detach old tracker from old value and remove it
                oldValue = trackerToken.Tracker.TrackedObject;
                trackerToken.Dispose();
                _propertyTrackers.Remove(propertyName);
            }

            if (newValue != null)
            {
                // Add a new tracker for the new value
                var token = new TrackerToken(newValue, propertyName, Tracker);
                _propertyTrackers.Add(propertyName, token);
            }

            // Notify that property changed
            if (raiseEvent)
                Tracker.RaisePropertyPathChanged(new PropertyPathChangedEventArgs(propertyName, oldValue, newValue));
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Is called when a property on the observed object changes.
            // Remove old tracker and create a new tracker for the new value.
            // This will also raise a PropertyChanged event with a property path.
            RefreshPropertyTracker(e.PropertyName, raiseEvent: true);
        }
    }
}
