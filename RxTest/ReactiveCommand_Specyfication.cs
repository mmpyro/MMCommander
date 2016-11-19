using System;
using System.Windows.Input;
using NUnit.Framework;
using Ploeh.AutoFixture;
using RxFramework;

namespace RxTest
{
    [TestFixture]
    public class ReactiveCommand_Specyfication
    {
        static Fixture Any = new Fixture();

        [Test]
        public void Command_Test()
        {
            //Given
            string result = null;
            string expected = Any.Create<string>();
            ICommand command = new ReactiveCommand<string>(t => result = t, _ => true);
            //When
            bool canExecute = command.CanExecute(null);
            command.Execute(expected);
            //Then
            Assert.That(expected, Is.EqualTo(result));
            Assert.That(canExecute, Is.True);
        }

        [Test]
        public void CommandCanExecute_Test()
        {
            //Given
            string result = null;
            string expected = Any.Create<string>();
            ICommand command = new ReactiveCommand<string>(t => result = t, _ => false);
            //When
            bool canExecute = command.CanExecute(null);
            command.Execute(expected);
            //Then
            Assert.That(canExecute, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void CommandOnError_Test()
        {
            //Given
            const string message = "Reactive";
            Exception exception = null;
            ReactiveCommand<string> command = new ReactiveCommand<string>(t => { },_ => false,ex => exception = ex );
            //When
            command.OnError(new Exception(message));
            //Then
            Assert.NotNull(exception);
            Assert.That(exception.Message, Is.EqualTo(message));
        }

        [Test]
        public void CommandOnComplete_Test()
        {
            //Given
            bool completed = false;
            ReactiveCommand<string> command = new ReactiveCommand<string>(t => { }, _ => false, () =>
            {
                completed = true;
            });
            //When
            command.OnNext(Any.Create<string>());
            command.OnCompleted();
            //Then
            Assert.That(completed, Is.True);
        }

        [Test]
        public void FullCommand_Test()
        {
            //Given
            const string message = "Reactive";
            string result = null;
            bool completed = false;
            Exception exception = null;
            string expected = Any.Create<string>();
            var command = new ReactiveCommand<string>(t => result = t, _ => true,ex => exception = ex,() => completed = true );
            //When
            bool canExecute = command.CanExecute(null);
            command.Execute(expected);
            command.OnError(new Exception(message));
            command.OnCompleted();
            //Then
            Assert.True(canExecute);
            Assert.AreEqual(result, expected);
            Assert.That(exception.Message, Is.EqualTo(message));
            Assert.True(completed);
        }

        [Test]
        public void CommandEvents_Test()
        {
            //Given
            string result = null;
            ICommand command = new ReactiveCommand<string>(t => result = t, _ => true);
            //When
            command.CanExecuteChanged += CommandOnCanExecuteChanged;
            command.CanExecuteChanged -= CommandOnCanExecuteChanged;
        }

        private void CommandOnCanExecuteChanged(object sender, EventArgs eventArgs)
        {
            //Empty method
        }
    }
}