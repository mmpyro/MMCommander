using System;
using System.Windows.Input;

namespace RxFramework
{
    public class ReactiveCommand<T> : ICommand, IObserver<T>
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;
        private readonly Action<Exception> _onErroAction;
        private readonly Action _onComplete;


        public ReactiveCommand(Action<T> execute, Predicate<T> canExecute , Action<Exception> onErroAction, Action onComplete)
        {
            _execute = execute;
            _canExecute = canExecute;
            _onErroAction = onErroAction;
            _onComplete = onComplete;
        }

        public ReactiveCommand(Action<T> execute, Predicate<T> canExecute ) : this(execute, canExecute ,exception => { }, () => { })
        {
            
        }

        public ReactiveCommand(Action<T> execute, Predicate<T> canExecute, Action onComplete) : this(execute, canExecute ,exception => { },onComplete)
        {

        }

        public ReactiveCommand(Action<T> execute, Predicate<T> canExecute, Action<Exception> onErroAction) : this(execute, canExecute,onErroAction,() => {})
        {

        }

        public void OnNext(T value)
        {
            try
            {
                if (_canExecute(value))
                    _execute(value);
            }
            catch(Exception ex)
            {
                OnError(ex);
            }
        }

        public void OnError(Exception error)
        {
            _onErroAction(error);
        }

        public void OnCompleted()
        {
            _onComplete();
        }

        #region ICommand
        public bool CanExecute(object parameter)
        {
            return _canExecute((T) parameter);
        }

        public void Execute(object parameter)
        {
           OnNext((T) parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        #endregion
    }
}