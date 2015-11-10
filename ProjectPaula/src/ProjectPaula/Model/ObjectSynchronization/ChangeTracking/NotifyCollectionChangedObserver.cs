using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    /// <summary>
    /// Subscribes to the <see cref="INotifyCollectionChanged.CollectionChanged"/> event
    /// of an object to determine collection changes.
    /// </summary>
    class NotifyCollectionChangedObserver : ObjectObserverBase<INotifyCollectionChanged>
    {
        // This is for the items in the observed collection
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private List<TrackerToken> _itemTrackers =
            new List<TrackerToken>();

        protected override void Attach()
        {
            if (!(TrackedObject is IEnumerable))
                throw new ArgumentException($"Failed to track collection changes on object of type {TrackedObject.GetType()}. The object does not implement {nameof(IEnumerable)}.");

            TrackedObject.CollectionChanged += OnCollectionChanged;

            // Scan through the items and create an ObjectTracker
            // for each item.
            var i = 0;
            foreach (var item in TrackedObject as IEnumerable)
                InsertTrackerAt(i++, item);
        }

        protected override void Detach()
        {
            TrackedObject.CollectionChanged -= OnCollectionChanged;
        }

        private void RemoveTrackerAt(int index)
        {
            if (index < _itemTrackers.Count)
            {
                var trackerToken = _itemTrackers[index];
                trackerToken.Dispose();
                _itemTrackers.RemoveAt(index);
            }
        }

        private void RemoveTrackerOf(object item)
        {
            var index = _itemTrackers.IndexOf(_itemTrackers.FirstOrDefault(t => t.Tracker.TrackedObject == item));
            RemoveTrackerAt(index);
        }

        private void InsertTrackerAt(int index, object item)
        {
            var token = new TrackerToken(item, index.ToString(), Tracker);
            _itemTrackers.Insert(index, token);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    while (_itemTrackers.Any())
                        RemoveTrackerAt(0);

                    Tracker.RaiseCollectionPathChanged(CollectionPathChangedEventArgs.CreateResetArgs());
                    break;

                case NotifyCollectionChangedAction.Add:
                    var index = (e.NewStartingIndex == -1) ? _itemTrackers.Count : e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                        InsertTrackerAt(index++, item);

                    Tracker.RaiseCollectionPathChanged(new CollectionPathChangedEventArgs(CollectionPathChangedAction.Add, e.NewItems, e.NewStartingIndex));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                        RemoveTrackerOf(item);

                    Tracker.RaiseCollectionPathChanged(new CollectionPathChangedEventArgs(CollectionPathChangedAction.Remove, e.OldItems, e.OldStartingIndex));
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // Remove old items...
                    foreach (var item in e.OldItems)
                        RemoveTrackerOf(item);

                    Tracker.RaiseCollectionPathChanged(new CollectionPathChangedEventArgs(CollectionPathChangedAction.Remove, e.OldItems, e.OldStartingIndex));

                    // ...and insert replacement items
                    var i = (e.NewStartingIndex == -1) ? _itemTrackers.Count : e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                        InsertTrackerAt(i++, item);

                    Tracker.RaiseCollectionPathChanged(new CollectionPathChangedEventArgs(CollectionPathChangedAction.Add, e.NewItems, e.NewStartingIndex));
                    break;

                case NotifyCollectionChangedAction.Move:
                    // Remove items at old indices...
                    foreach (var item in e.OldItems)
                        RemoveTrackerOf(item);

                    Tracker.RaiseCollectionPathChanged(new CollectionPathChangedEventArgs(CollectionPathChangedAction.Remove, e.OldItems, e.OldStartingIndex));

                    // ...and re-insert them at the new indices
                    var i2 = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                        InsertTrackerAt(i2++, item);

                    Tracker.RaiseCollectionPathChanged(new CollectionPathChangedEventArgs(CollectionPathChangedAction.Add, e.NewItems, e.NewStartingIndex));
                    break;
            }
        }
    }
}
