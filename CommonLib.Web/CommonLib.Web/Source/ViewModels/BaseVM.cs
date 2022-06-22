using System.ComponentModel;
using CommonLib.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.ViewModels
{
    public abstract class BaseVM : INotifyPropertyChanged, INotifyPropertyChangedHelper
    {
        public void SetPropertyAndNotify<T>(ref T field, T propVal, string propName)
        {
            if (Equals(field, propVal)) return;
            field = propVal;
            OnPropertyChanging(propName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanging(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
