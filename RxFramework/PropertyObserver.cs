using System;
using System.Linq.Expressions;

namespace RxFramework
{
    public class PropertyObserver<U,T> : ReactiveObject where U : RaisePropertyChanged
    {
        private readonly Expression<Func<U,T>> _lambda;
        private readonly IObservable<T> _observable;
        private T _value;

        public PropertyObserver(IObservable<T> observable, U parent, Expression<Func<U,T>> lambda)
        {
            _observable = observable;
            _lambda = lambda;
            observable.Subscribe(x =>
            {
                _value = x;
                string name = lambda.GetMemberName();
                parent.OnPropertyChanged(name);
            });
        }
        public T Value
        {
            get
            {
                return _value;
            }
        }

    }
}
