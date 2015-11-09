using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;

namespace ProjectPaula.Model.ObjectSynchronization
{
    /// <summary>
    /// Specifies methods that need to be implemented by SignalR
    /// clients (e.g. JavaScript clients) in order to enable
    /// object synchronization.
    /// </summary>
    public interface IObjectSynchronizationClient
    {
        void InitializeObject(string key, object o);
        void PropertyChanged(string key, PropertyPathChangedEventArgs e);
        void CollectionChanged(string key, CollectionPathChangedEventArgs e);
    }
}
