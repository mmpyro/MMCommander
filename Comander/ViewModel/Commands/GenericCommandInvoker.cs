using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Comander.ViewModel.Commands
{
    public class GenericCommandInvoker
    {
        private readonly IOManager _ioManager1;
        private readonly IOManager _ioManager2;

        public GenericCommandInvoker(IOManager ioManager1, IOManager ioManager2)
        {
            _ioManager1 = ioManager1;
            _ioManager2 = ioManager2;
        }

        // Parameter format: "<program> -args <arg> <arg>...", where {s}/{d} are the files selected in the left/right panel.
        public void Execute(string parameter)
        {
            if (_ioManager1.SelectedFile == null || _ioManager2.SelectedFile == null)
                throw new InvalidOperationException("Select a file in both panels first.");
            string[] param = parameter.Split("-args", 2);
            var info = new ProcessStartInfo(param[0].Trim());
            if (param.Length > 1)
                foreach (var token in param[1].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    info.ArgumentList.Add(token.Replace("{s}", _ioManager1.SelectedFile.FullName).Replace("{d}", _ioManager2.SelectedFile.FullName));
            Process.Start(info);
        }
    }

    public class GenericCommand : ICommand
    {
        private readonly GenericCommandInvoker _invoker;
        public string Name { get; set; }
        public string Parameter { get; set; }

        public GenericCommand(string name, string parameter, GenericCommandInvoker invoker)
        {
            Name = name;
            Parameter = parameter;
            _invoker = invoker;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _invoker.Execute(Parameter);
        }

        #pragma warning disable
        public event EventHandler CanExecuteChanged;
        #pragma warning restore
    }
}
