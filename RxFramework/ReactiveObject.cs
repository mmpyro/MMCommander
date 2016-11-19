using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using RxFramework.Annotations;

namespace RxFramework
{
    public abstract class RaisePropertyChanged : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public virtual void RaiseIfChanged<T>(ref T reference, T value, [CallerMemberName] string propertyName = null)
        {
            if(!reference.Equals(value))
            {
                reference = value;
                OnPropertyChanged(propertyName);
            }
        }
    }

    public  class ReactiveObject : RaisePropertyChanged
    {
        public IObservable<T> ObservableFromProperty<U,T>(U parent, Expression<Func<U,T>> lambda)
        {

            string propertyName = lambda.GetMemberName();
            var fun = lambda.Compile();
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => handler.Invoke,
                h => PropertyChanged += h,
                h => PropertyChanged -= h)
                .Where(t => StringEquals(t.EventArgs.PropertyName, propertyName))
                .Select(_ => fun(parent));
        }

        protected bool StringEquals(string first, string second)
        {
            return first.Equals(second, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}