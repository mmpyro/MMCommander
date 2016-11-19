using System;
using System.Linq.Expressions;

namespace RxFramework
{
    public static class ReactiveExtension
    {
        public static IDisposable InvokeCommand<T>(this IObservable<T> source, IObserver<T>  observer)
        {
            return source.Subscribe(observer);
        }

        public static PropertyObserver<U,T> ToProperty<U,T>(this IObservable<T> observable, U parent, Expression<Func<U,T>> lambda) where U : RaisePropertyChanged
        {
            return new PropertyObserver<U,T>(observable, parent, lambda);
        }
    }
}