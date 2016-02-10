using System;
using System.Diagnostics;
using System.Windows.Input;
using LogLib;

namespace Comander.ViewModel.Commands
{
    public class GenericCommandInvoker
    {
        private readonly IOManager _ioManager1;
        private readonly IOManager _ioManager2;
        private readonly ILogger _logger;

        public GenericCommandInvoker(IOManager ioManager1, IOManager ioManager2, ILogger logger)
        {
            _ioManager1 = ioManager1;
            _ioManager2 = ioManager2;
            _logger = logger;
        }

        public void Execute(string parameter)
        {
            string source = _ioManager1.SelectedFile.FullName;
            string destination = _ioManager2.SelectedFile.FullName;
            string[] param = parameter.Replace("-args", "#").Split('#');
            string performedProcessName = param[1].Replace("{s}", source).Replace("{d}", destination);
            Process.Start(param[0], performedProcessName);
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