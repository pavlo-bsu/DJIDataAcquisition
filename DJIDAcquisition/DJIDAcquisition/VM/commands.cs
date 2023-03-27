using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Pavlo.DJIDAcquisition.VM
{
    public class ConnectCommand : ICommand
    {
        private ViewModel viewModel;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            viewModel.ConnectCmdAction();
        }

        public ConnectCommand(ViewModel vm)
        {
            viewModel = vm;
        }
    }

    public class StartReceivingCommand: ICommand
    {
        private ViewModel viewModel;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            viewModel.StartReceivingAction();
        }

        public StartReceivingCommand(ViewModel vm)
        {
            viewModel = vm;
        }
    }

    public class StopReceivingCommand : ICommand
    {
        private ViewModel viewModel;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            viewModel.StopRecievingAction();
        }

        public StopReceivingCommand(ViewModel vm)
        {
            viewModel = vm;
        }
    }
}
