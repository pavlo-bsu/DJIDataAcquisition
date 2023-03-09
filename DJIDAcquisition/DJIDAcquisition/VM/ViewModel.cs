using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Pavlo.DJIDAcquisition.Model;

namespace Pavlo.DJIDAcquisition.VM
{
    public class ViewModel:INPCBaseDotNet4_5
    {
        #region PropertyNames for INPC
        public static readonly string PropertyNameMSG = "MSG";
        #endregion

        private ObservableCollection<DJIRecord> _RecordsList;
        public ObservableCollection<DJIRecord> RecordList
        {
            get { return _RecordsList; }
            set { _RecordsList = value; }
        }

        private object recordListLock;

        private string _MSG = string.Empty;
        public string MSG
        {
            get
            {
                return _MSG;
            }
            set
            {
                _MSG = value;
                NotifyPropertyChanged();
            }
        }

        private DroneState _droneState;
        private DroneState droneState
        { 
            get { return _droneState; }
            set
            {
                _droneState = value;
                switch (droneState)
                {
                    case DroneState.AppNotRegistered:
                        MSG = "App is not registred";
                        break;
                    case DroneState.AppRegistered:
                        MSG = "App is registred";
                        break ;
                    case DroneState.DroneDisconnected:
                        MSG = "Drone is disconnected";
                        break;
                    case DroneState.DroneConnected:
                        MSG = "Drone is connected";
                        break;
                    case DroneState.DJISDKFailed:
                        MSG = "DJI SDK Failed";
                        break;
                    default:
                        MSG = "Unknown state";
                        break;
                }

            }
        }

        public ViewModel ()
        {
            droneState = DroneState.AppNotRegistered;

            RecordList = new ObservableCollection<DJIRecord>();

            // https://stackoverflow.com/questions/2091988/how-do-i-update-an-observablecollection-via-a-worker-thread
            // https://stackoverflow.com/questions/21720638/using-bindingoperations-enablecollectionsynchronization
            recordListLock = new object();

            DJIRecord record = new DJIRecord() { ID = 0, Description = "starting", Date= System.DateTime.Now };
            RecordList.Add(record);
        }

    }

    public class INPCBaseDotNet4_5 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        //.net4.5
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public INPCBaseDotNet4_5()
        { }
    }
    

    /// <summary>
    /// describe states of drone 
    /// </summary>
    public enum DroneState
    {
        AppNotRegistered,
        AppRegistered,
        DroneDisconnected,
        DroneConnected,
        DJISDKFailed,
    }
}
