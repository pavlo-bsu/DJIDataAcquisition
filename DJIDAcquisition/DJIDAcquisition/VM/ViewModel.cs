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
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).VelocityChanged += ComponentHandingPage_VelocityChanged;


            //Emulation of events

            DJIRecord record = new DJIRecord() { ID = 0, Description = "starting", Date = System.DateTime.Now, Type = "fakeEvent" };
            RecordList.Add(record);
            DJIRecord record2 = new DJIRecord() { ID = 1, Description = "some event (main thread)", Date = System.DateTime.Now };
            RecordList.Add(record2);

            int tasksCount = 5;
            int averDelay_ms = 200;
            
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
                        //RecordList.Add(recordT);  // <- should be in UI thread
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
            string tmpDescroption = $"({value.Value.x}, {value.Value.y}, {value.Value.z}). Abs={Math.Sqrt(value.Value.x* value.Value.x+ value.Value.y* value.Value.y + value.Value.z* value.Value.z)}.";
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
