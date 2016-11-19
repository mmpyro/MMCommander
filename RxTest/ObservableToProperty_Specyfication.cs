using NUnit.Framework;
using Ploeh.AutoFixture;
using RxFramework;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxTest
{
    [TestFixture]
    public class ObservableToProperty_Specyfication
    {
        static Fixture Any = new Fixture();

        [Test]
        public void ShouldNotifyViewModel()
        {
            //Given
            Vm vm = new Vm(Any.Create<string>());
            int value = Any.Create<int>();
            //When
            vm.ObserverPropertyValue = Observable.Create<int>(obs =>
            {
                obs.OnNext(value);
                obs.OnCompleted();
                return Disposable.Empty;
            }).ToProperty(vm, t => t.SecondValue);
            //Then
            Assert.That(vm.SecondValue, Is.EqualTo(value));
        }
    }
}
