using System;
using System.Diagnostics;
using System.Windows.Input;
using Comander.Core;
using LogLib;

namespace Comander.ViewModel.Commands
{
    public class GenericCommandInvoker
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Parameter { get; set; }


        public GenericCommandInvoker(string name, string key, string parameter)
        {
            Name = name;
            Key = key;
            Parameter = parameter;
        }

        public void Execute(string sourcePath, string destinationPath)
        {
            string[] param = Parameter.Replace("-args", "#").Split('#');
            string performedProcessName = param[1].Replace("{s}", sourcePath).Replace("{d}", destinationPath);
            Process.Start(param[0], performedProcessName);
        }
    }

    public class GenericCommand : ICommand
    {
        private readonly IOManager _ioManager1;
        private readonly IOManager _ioManager2;
        private readonly ILogger _logger;
        private readonly GenericCommandManager _comamndManager;

        public GenericCommand( IOManager ioManager1, IOManager ioManager2,ILogger logger)
        {
            _ioManager1 = ioManager1;
            _ioManager2 = ioManager2;
            _logger = logger;
            _comamndManager = Locator.GenericCommandManager;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            try
            {
                string key = parameter.ToString();
                GenericCommandInvoker genericCommand;
                if (key.Contains("Name:"))
                    genericCommand = _comamndManager.GetCommandByName(key.Replace("Name:", ""));
                else
                    genericCommand = _comamndManager.GetCommandByKey(key.Replace("Key:", ""));
                genericCommand.Execute(_ioManager1.SelectedFile.FullName, _ioManager2.SelectedFile.FullName);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

        }

        public event EventHandler CanExecuteChanged;
    }

}