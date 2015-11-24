using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        /// The property value before the change.
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// The property value after the change.
        /// </summary>
        public object NewValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyPathChangedEventArgs"/> class.
        /// </summary>
        /// <param name="propertyPath">Path to the property that changed</param>
        /// <param name="oldValue">Old property value</param>
        /// <param name="newValue">New property value</param>
        public PropertyPathChangedEventArgs(string propertyPath, object oldValue, object newValue)
        {
            PropertyPath = propertyPath;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
