using RxFramework;

namespace RxTest
{
    internal class Vm : ReactiveObject
    {
        private string _value;
        public PropertyObserver<Vm, int> ObserverPropertyValue { get; set; }


        public Vm(string value)
        {
            _value = value;
        }

        public string FirstValue
        {
            get { return _value; }
            set
            {
                RaiseIfChanged(ref _value, value);
            }
        }

        public int SecondValue
        {
            get
            {
                return ObserverPropertyValue.Value;
            }
        }

    }
}