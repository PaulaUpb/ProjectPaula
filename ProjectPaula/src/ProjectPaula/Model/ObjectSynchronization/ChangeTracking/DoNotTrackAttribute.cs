using System;

namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    /// <summary>
    /// Excludes the attributed property from change tracking.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class DoNotTrackAttribute : Attribute
    {
    }
}
