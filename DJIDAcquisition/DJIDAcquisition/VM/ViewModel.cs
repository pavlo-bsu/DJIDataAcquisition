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
using Windows.Storage;
using System.IO;

namespace Pavlo.DJIDAcquisition.VM
{
    public class ViewModel:INPCBaseDotNet4_5
    {
        #region PropertyNames for INPC
        public static readonly string PropertyNameMSG = "MSG";
        public static readonly string PropertyIsReceiving = "IsReceiving";
        #endregion

        private ObservableCollection<DJIRecord> _RecordsList;
        public ObservableCollection<DJIRecord> RecordList
        {
            get { return _RecordsList; }
            set { _RecordsList = value; }
        }

        /// <summary>
        /// lock for RecordList collection
        /// </summary>
        private object recordListLock;

        private string _MSG = string.Empty;
        /// <summary>
        /// general message about app and drone state
        /// </summary>
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
        /// <summary>
        /// drone / app state
        /// </summary>
        public DroneState droneState
        { 
            get { return _droneState; }
            private set
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

        public StopReceivingCommand StopReceivingCommand
        {
            get;
            private set;
        }

        private bool _IsReceiving = false;
        /// <summary>
        /// show state of the app: whether app receiving data from drone or not
        /// </summary>
        public bool IsReceiving
        {
            get { return _IsReceiving; }
            private set
            {
                _IsReceiving = value;
                NotifyPropertyChanged();
            }
        }

        public ViewModel ()
        {
            ConnectCmd = new ConnectCommand(this);
            StartReceivingCmd = new StartReceivingCommand(this);
            StopReceivingCommand = new StopReceivingCommand(this);

            droneState = DroneState.AppNotRegistered;

            RecordList = new ObservableCollection<DJIRecord>();

			//For WPF, not for UWP
            // https://stackoverflow.com/questions/2091988/how-do-i-update-an-observablecollection-via-a-worker-thread
            // https://stackoverflow.com/questions/21720638/using-bindingoperations-enablecollectionsynchronization
            recordListLock = new object();
        }

        /// <summary>
        /// Action for ConnectCommand: registering the App
        /// </summary>
        public void ConnectCmdAction()
        {
            DJISDKManager.Instance.SDKRegistrationStateChanged += Instance_SDKRegistrationEvent;

            //get DJI ID from the file 
            var t = Task.Factory.StartNew(() => GetDJIKeyAsync().GetAwaiter().GetResult());
            string id = t.GetAwaiter().GetResult();

            //register the app
            DJISDKManager.Instance.RegisterApp(id);
        }

        /// <summary>
        /// Get DJI key from file embedded into the project
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetDJIKeyAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///DJIkey.txt"));
            using (var inputStream = await file.OpenReadAsync())
            using (var classicFS = inputStream.AsStreamForRead())
            using (var classicSR = new StreamReader(classicFS,Encoding.ASCII))
            {
                return await classicSR.ReadLineAsync();
            }
        }

        /// <summary>
        /// Action for StopRecievingCommand: write log to file
        /// </summary>
        public void StopRecievingAction()
        {
            //unsubscribe from drone events
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).VelocityChanged -= ComponentHandingPage_VelocityChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).SatelliteCountChanged += ViewModel_SatelliteCountChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AircraftLocationChanged -= ViewModel_AircraftLocationChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AreMotorsOnChanged -= ViewModel_AreMotorsOnChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GPSSignalLevelChanged -= ViewModel_GPSSignalLevelChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).CompassHasErrorChanged -= ViewModel_CompassHasErrorChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).ESCHasErrorChanged -= ViewModel_ESCHasErrorChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).HasNoEnoughForceChanged -= ViewModel_HasNoEnoughForceChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GPSModeFailureReasonChanged -= ViewModel_GPSModeFailureReasonChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).CompassInstallErrorChanged -= ViewModel_CompassInstallErrorChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).WindWarningChanged -= ViewModel_WindWarningChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).IsMotorStuckChanged -= ViewModel_IsMotorStuckChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).FailsafeActionChanged -= ViewModel_FailsafeActionChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).ConnectionChanged -= ViewModel_ConnectionChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AirSenseSystemConnectedChanged -= ViewModel_AirSenseSystemConnectedChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AirSenseSystemInformationChanged -= ViewModel_AirSenseSystemInformationChanged;
            //RC
            DJISDKManager.Instance.ComponentManager.GetRemoteControllerHandler(0, 0).ConnectionChanged -= ViewModel_RC_ConnectionChanged;

            //Camera
            DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).ConnectionChanged -= ViewModel_CameraConnectionChanged;


            //writing log to file
            try
            {
                var t = Task.Factory.StartNew(() => WriteEventsToLog());
                t.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            { }
            finally
            {
                IsReceiving= false;
            }
        }

        /// <summary>
        /// write all the events into a log file
        /// </summary>
        /// <returns></returns>
        private async Task WriteEventsToLog()
        {
            string[] strs = null;
            lock (recordListLock)
            {
                strs = new string[RecordList.Count];
                for (int i = 0; i < strs.Length; i++)
                {
                    strs[i] = RecordList[i].ToString();
                }
            }

            if (strs != null)
            {
                string fileName = System.DateTime.Now.ToString("yMMdd_HHmm") + ".dji";
                StorageFile file = await DownloadsFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                await FileIO.WriteLinesAsync(file, strs, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            }
            return;
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

        /// <summary>
        /// Action for StartRecievingCommand
        /// </summary>
        public void StartReceivingAction()
        {
            //indication of receiving is started
            IsReceiving = true;

            //clear all elements in the list
            lock(recordListLock)
            {
                RecordList.Clear();
            }

            //events subscription
            //https://developer.dji.com/api-reference/windows-api/Components/ComponentManager.html
            
            //FlightController
            //DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).VelocityChanged += ComponentHandingPage_VelocityChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AircraftLocationChanged += ViewModel_AircraftLocationChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).SatelliteCountChanged += ViewModel_SatelliteCountChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AreMotorsOnChanged += ViewModel_AreMotorsOnChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GPSSignalLevelChanged += ViewModel_GPSSignalLevelChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).CompassHasErrorChanged += ViewModel_CompassHasErrorChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).ESCHasErrorChanged += ViewModel_ESCHasErrorChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).HasNoEnoughForceChanged += ViewModel_HasNoEnoughForceChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GPSModeFailureReasonChanged += ViewModel_GPSModeFailureReasonChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).CompassInstallErrorChanged += ViewModel_CompassInstallErrorChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).WindWarningChanged += ViewModel_WindWarningChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).IsMotorStuckChanged += ViewModel_IsMotorStuckChanged;
            //changes of the FailSafe action (gohome, landing, hover etc) for when the connection between remote controller and aircraft is lost.
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).FailsafeActionChanged += ViewModel_FailsafeActionChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).ConnectionChanged += ViewModel_ConnectionChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AirSenseSystemConnectedChanged += ViewModel_AirSenseSystemConnectedChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AirSenseSystemInformationChanged += ViewModel_AirSenseSystemInformationChanged;
            
            //RC
            DJISDKManager.Instance.ComponentManager.GetRemoteControllerHandler(0, 0).ConnectionChanged += ViewModel_RC_ConnectionChanged;
            
            //Camera
            DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).ConnectionChanged += ViewModel_CameraConnectionChanged;
        }

        private async void ViewModel_RC_ConnectionChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "RC_ConnectionChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_AirSenseSystemInformationChanged(object sender, AirSenseSystemInformation? value)
        {
            string tmpDescroption = value == null ? "null" : $"warningLevel is {value.Value.warningLevel}.There are info in property 'airplaneStates'";
            //if necessary, add info from value.Value.airplaneStates

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "AirSenseSystemInformationChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_AirSenseSystemConnectedChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "AirSenseSystemConnectedChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_ConnectionChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "FlightController: ConnectionChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_FailsafeActionChanged(object sender, FCFailsafeActionMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "FailsafeActionChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_IsMotorStuckChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "IsMotorStuckChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_WindWarningChanged(object sender, FCWindWarningMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "CompassInstallErrorChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_CompassInstallErrorChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "CompassInstallErrorChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_GPSModeFailureReasonChanged(object sender, FCGPSModeFailureReasonMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "GPSModeFailureReasonChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_HasNoEnoughForceChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "HasNoEnoughForceChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_ESCHasErrorChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "ESCHasErrorChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_CompassHasErrorChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value==null?"null":$"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "CompassHasErrorChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_GPSSignalLevelChanged(object sender, FCGPSSignalLevelMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "GPSSignalLevelChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_AreMotorsOnChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "AreMotorsOnChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_SatelliteCountChanged(object sender, IntMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "SatelliteCountChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_CameraConnectionChanged(object sender, BoolMsg? value)
        {
            string tmpDescroption = value == null ? "null" : $"{value.Value.value}";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "CameraConnectionChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        private async void ViewModel_AircraftLocationChanged(object sender, LocationCoordinate2D? value)
        {
            string tmpDescroption = value == null ? "null" : $"({value.Value.latitude}, {value.Value.longitude}).";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "AircraftLocationChanged" };
                    RecordList.Add(recordT);
                }
            });
        }

        

        private async void ComponentHandingPage_VelocityChanged(object sender, Velocity3D? value)
        {
            string tmpDescroption = value == null ? "null" : $"({value.Value.x}, {value.Value.y}, {value.Value.z}). Abs={Math.Sqrt(value.Value.x * value.Value.x + value.Value.y * value.Value.y + value.Value.z * value.Value.z)}.";

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lock (recordListLock)
                {
                    DJIRecord recordT = new DJIRecord() { ID = RecordList.Count, Description = tmpDescroption, Date = System.DateTime.Now, Type = "VelocityChanged" };
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
    /// describes states of drone 
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
