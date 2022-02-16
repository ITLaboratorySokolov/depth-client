using System.ComponentModel;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    /// <summary>
    /// Abstract class able to notify about changes of its properties.
    /// </summary>
    public abstract class NotifyingClass : INotifyPropertyChanged
    {
        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise the property changed event.
        /// </summary>
        /// <param name="property">Name of the a property.</param>
        protected void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
