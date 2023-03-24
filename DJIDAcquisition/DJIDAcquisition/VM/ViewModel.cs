using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DJI.WindowsSDK;
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

        public ConnectCommand ConnectCmd
        {
            get;
            private set;
        }

        public StartReceivingCommand StartReceivingCmd
        {
            get;
            private set;
        }

        public ViewModel ()
        {
            ConnectCmd = new ConnectCommand(this);
            StartReceivingCmd = new StartReceivingCommand(this);

            droneState = DroneState.AppNotRegistered;

            RecordList = new ObservableCollection<DJIRecord>();

			//For WPF, not for UWP
            // https://stackoverflow.com/questions/2091988/how-do-i-update-an-observablecollection-via-a-worker-thread
            // https://stackoverflow.com/questions/21720638/using-bindingoperations-enablecollectionsynchronization
            recordListLock = new object();

            DJIRecord record = new DJIRecord() { ID = 0, Description = "starting", Date= System.DateTime.Now, Type="fakeEvent" };
            RecordList.Add(record);
        }

        /// <summary>
        /// Action for ConnectCommand: registering the App
        /// </summary>
        public void ConnectCmdAction()
        {
            //string appDJIID = System.IO.File.ReadAllText(@"d:\k.txt");

            DJISDKManager.Instance.SDKRegistrationStateChanged += Instance_SDKRegistrationEvent;

            DJISDKManager.Instance.RegisterApp("ID!!!");
        }

        /// <summary>
        /// Handling of SDK registration event
        /// </summary>
        /// <param name="state"></param>
        /// <param name="resultCode"></param>
        private async void Instance_SDKRegistrationEvent(SDKRegistrationState state, SDKError resultCode)
        {
            if (resultCode == SDKError.NO_ERROR)
            {
                //https://social.msdn.microsoft.com/Forums/en-US/dbb45169-c30d-42e6-bfb1-1869c7d70736/what-could-i-do-to-execute-code-on-ui-thread-when-the-app-is-resuming-from-suspending-state?forum=winappswithcsharp
                var t = Task.Factory.StartNew(
                          () =>
                          {
                              //why code doesn't work (there is a UI thread lock?) without enveloping into a task
                              Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   droneState = DroneState.AppRegistered;
                               });
                          });
                await t;
                
                //The product connection state will be updated when it changes here.
                DJISDKManager.Instance.ComponentManager.GetProductHandler(0).ProductTypeChanged += ProductTypeChangedEvent;
            }
            else
            {
                var t = Task.Factory.StartNew(
                          () =>
                          {
                              Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   droneState = DroneState.DJISDKFailed;
                               });
                          });
                await t;
            }
        }

        /// <summary>
        /// Handling of drone connection or disconnection 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private async void ProductTypeChangedEvent (object sender, ProductTypeMsg? value)
        {
             await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (value != null && value?.value != ProductType.UNRECOGNIZED)
                    {
                        droneState = DroneState.DroneConnected;
                        //You can load/display your pages according to the aircraft connection state here.
                    }
                    else
                    {
                        droneState = DroneState.DroneDisconnected;
                        //System.Diagnostics.Debug.WriteLine("The Aircraft is disconnected now.");
                        //You can hide your pages according to the aircraft connection state here, or show the connection tips to the users.
                    }
                });
        }

    public void StartReceivingAction()
        {
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).VelocityChanged += ComponentHandingPage_VelocityChanged;

            int id = 1;
            DJIRecord record = new DJIRecord() { ID = id++, Description = "some event (main thread)", Date = System.DateTime.Now };
            RecordList.Add(record);

            int tasksCount = 5;
            int averDelay_ms = 1000;
            
            Task[] tasks = new Task[tasksCount];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new Task(()=>
                {
                    //payload
                    Thread.Sleep(averDelay_ms);

                    lock (recordListLock)
                    {
                        DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = $"event in a task", Date = System.DateTime.Now };
                        //RecordList.Add(recordT);
                    }
                } );
            }

            for (int i = 0; i < tasks.Length; i++)
                tasks[i].Start();

            Task.WaitAll(tasks);

            DJIRecord recordEnd = new DJIRecord() { ID = RecordList.Count, Description = "some event (main thread)", Date = System.DateTime.Now };
            RecordList.Add(recordEnd);
            
        }

        private async void ComponentHandingPage_VelocityChanged(object sender, Velocity3D? value)
        {
            string tmpDescroption = $"{value.Value.x}, {value.Value.y}, {value.Value.z}. Total value={Math.Sqrt(value.Value.x* value.Value.x+ value.Value.y* value.Value.y + value.Value.z* value.Value.z)}.";
            DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "VelocityChanged" };

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    RecordList.Add(recordT);
                }
            });
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
