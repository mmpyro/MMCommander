using Comander.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Comander.ViewModel.Commands
{
    public class RunCommand : ICommand
    {
        private readonly ConfigurationProgram _config;

        public RunCommand(ConfigurationProgram config)
        {
            _config = config;
        }
#pragma warning disable
        public event EventHandler CanExecuteChanged;
#pragma warning restore

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var processInfo = new ProcessStartInfo(_config.Value);
            processInfo.WorkingDirectory = _config.WorkingDir ?? @"C:\";
            Process.Start(processInfo);
        }
    }
}
