using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VSToolsForKSP.Settings
{
    public class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Event required for INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Call this when setting a property instead of the basic set method
        /// Fires INotifyPropertyChanged event on changed
        /// </summary>
        /// <typeparam name="T">Type of property</typeparam>
        /// <param name="field">The field getting set</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Name for notification event, optional cause <see cref="CallerMemberNameAttribute" /> will pick this up</param>
        /// <returns></returns>
        protected bool Set<T>(ref T field, T value, [CallerMemberName]string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            field = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">Name for notification event, optional cause <see cref="CallerMemberNameAttribute" /> will pick this up</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
