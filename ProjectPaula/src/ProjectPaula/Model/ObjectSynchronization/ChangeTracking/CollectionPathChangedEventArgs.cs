using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    public delegate void CollectionPathChangedEventHandler(ObjectTracker sender, CollectionPathChangedEventArgs e);

    public class CollectionPathChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Path to the collection property that changed.
        /// </summary>
        public string PropertyPath { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CollectionPathChangedAction Action { get; }

        /// <summary>
        /// The items that have been added or removed.
        /// </summary>
        public IList Items { get; }

        /// <summary>
        /// The index of the first item that has been added or removed.
        /// </summary>
        public int StartingIndex { get; }

        public static CollectionPathChangedEventArgs CreateResetArgs()
            => new CollectionPathChangedEventArgs(CollectionPathChangedAction.Reset, null, -1);

        public CollectionPathChangedEventArgs(CollectionPathChangedAction action, IList items, int startingIndex)
        {
            // Initially the property path is always empty
            PropertyPath = "";

            Action = action;
            Items = items;
            StartingIndex = startingIndex;
        }
    }

    public enum CollectionPathChangedAction
    {
        Add,
        Remove,
        Reset
    }
}
