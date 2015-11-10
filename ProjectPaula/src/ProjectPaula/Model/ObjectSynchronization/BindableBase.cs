using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectPaula.Model.ObjectSynchronization
{
    /// <summary>
    /// A base class for ViewModels implementing <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets the specified variable to the specified value
        /// and raises the <see cref="PropertyChanged"/> event
        /// for the calling property if necessary.
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="storage">Variable</param>
        /// <param name="value">Value</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>True if the value changed; false if the specified value is already the current value</returns>
        protected bool Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (!Equals(storage, value))
            {
                storage = value;
                RaisePropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            var args = new PropertyChangedEventArgs(propertyName);
            PropertyChanged?.Invoke(this, args);
        }
    }
}
