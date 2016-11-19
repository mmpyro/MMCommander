using System;
using System.Reactive.Linq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using RxFramework;

namespace RxTest
{
    [TestFixture]
    public class PropertyToObservable_Specyfication
    {
        static Fixture Any = new Fixture();

        [Test]
        public void Basic_Notification()
        {
            //Given
            string result = "";
            string input = Any.Create<string>();
            var vm = new Vm("");
            vm.ObservableFromProperty(vm,t => t.FirstValue)
                .DistinctUntilChanged()
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.ToUpper())
                .InvokeCommand(new ReactiveCommand<string>(t => result = t, _ => true));
            //When
            vm.FirstValue = input;
            //Then
            Assert.That(result, Is.EqualTo(input.ToUpper()));
        }

        [Test]
        public void EmptyString_Test()
        {
            //Given
            string result = null;
            var vm = new Vm(Any.Create<string>());
            vm.ObservableFromProperty(vm,t => t.FirstValue)
                .DistinctUntilChanged()
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .InvokeCommand(new ReactiveCommand<string>(t => result = t, _ => true));
            //When
            vm.FirstValue = string.Empty;
            //Then
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Distinct_Tets()
        {
            //Given
            string result = null;
            string input = Any.Create<string>();
            var vm = new Vm(input);
            vm.ObservableFromProperty(vm,t => t.FirstValue)
            .DistinctUntilChanged()
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .InvokeCommand(new ReactiveCommand<string>(t => result = t, _ => true));
            //When
            vm.FirstValue = input;
            //Then
            Assert.That(result, Is.Null);
        }

        [Test]
        public void InvalidLambda_Test()
        {
            //Given
            string input = Any.Create<string>();
            var vm = new Vm(input);
            //When
            var ex = Assert.Throws<ArgumentException>(() => vm.ObservableFromProperty<Vm, string>(vm, t => null));
            //Then
            Assert.That(ex.Message, Is.EqualTo("Lambda must have body"));
        }

        [Test]
        public void Dispose_Test()
        {
            //Given
            string result = null;
            var vm = new Vm( Any.Create<string>());
            IDisposable disp = vm.ObservableFromProperty(vm,t => t.FirstValue)
            .DistinctUntilChanged()
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .InvokeCommand(new ReactiveCommand<string>(t => result = t, _ => true));
            //When
            string value1 = Any.Create<string>();
            string value2 = Any.Create<string>();
            vm.FirstValue = value1;
            disp.Dispose();
            vm.FirstValue = value2;
            //Then
            Assert.That(result, Is.EqualTo(value1));
        }

    }
}
