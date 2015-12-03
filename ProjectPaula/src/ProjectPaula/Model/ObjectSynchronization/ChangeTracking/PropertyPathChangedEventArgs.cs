using Newtonsoft.Json;
using System;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    public delegate void PropertyPathChangedEventHandler(ObjectTracker sender, PropertyPathChangedEventArgs e);

    public class PropertyPathChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Path to the property that changed.
        /// </summary>
        /// <example>
        /// "Address.City.ZipCode"
        /// "Company.Employees.3.Name" (which means "Company.Employees[3].Name")
        /// </example>
        public string PropertyPath { get; set; }

        /// <summary>
        /// Name of the property that changed.
        /// </summary>
        [JsonIgnore]
        public string PropertyName { get; }

        /// <summary>
        /// The object on which the property has changed.
        /// </summary>
        [JsonIgnore]
        public object Object { get; }

        /// <summary>
        /// The property value before the change.
        /// </summary>
        [JsonIgnore]
        public object OldValue { get; }

        /// <summary>
        /// The property value after the change.
        /// </summary>
        public object NewValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyPathChangedEventArgs"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        /// <param name="oldValue">Old property value</param>
        /// <param name="newValue">New property value</param>
        public PropertyPathChangedEventArgs(string propertyName, object obj, object oldValue, object newValue)
        {
            PropertyPath = propertyName;
            PropertyName = propertyName;
            Object = obj;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
