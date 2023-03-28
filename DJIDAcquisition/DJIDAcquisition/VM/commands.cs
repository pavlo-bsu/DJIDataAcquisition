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
            if (viewModel.droneState == DroneState.AppRegistered || viewModel.droneState == DroneState.DroneConnected)
            {
                return false;
            }
            return true;
        }

        public void Execute(object parameter)
        {
            viewModel.ConnectCmdAction();
        }

        public ConnectCommand(ViewModel vm)
        {
            viewModel = vm;
            vm.PropertyChanged += VM_PropertyChanged;
        }

        private void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ViewModel.PropertyNameMSG)
            {//i.e. drone status may have changed
                if (CanExecuteChanged != null)
                {
                    CanExecuteChanged(this, e);
                }
            }
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
