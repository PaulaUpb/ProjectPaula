namespace ProjectPaula.Model.ObjectSynchronization.ChangeTracking
{
    /// <summary>
    /// Provides notifications about property changes and collection changes
    /// anywhere in a tree of objects. The events provide full paths to the
    /// properties that changed.
    /// Implemented by <see cref="ObjectTracker"/>.
    /// </summary>
    public interface INotifyObjectChanged
    {
        event PropertyPathChangedEventHandler PropertyChanged;
        event CollectionPathChangedEventHandler CollectionChanged;
    }
}
