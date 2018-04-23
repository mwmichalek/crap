using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Maker.Serial;
using Microsoft.Maker.RemoteWiring;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Mwm.ConnectToIt.Services;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;

namespace Mwm.ConnectToIt {
    public sealed partial class App : Application {
        private Lazy<ActivationService> _activationService;

        DispatcherTimer timeout;
        // stopwatch for tracking connection timing
        Stopwatch connectionStopwatch = new Stopwatch();
        CancellationTokenSource cancelTokenSource;

        public static IStream Connection {
            get;
            set;
        }

        public static RemoteDevice Arduino {
            get;
            set;
        }

         private ActivationService ActivationService {
            get { return _activationService.Value; }
        }

        public App() {
            InitializeComponent();

            // Deferred execution until used. Check https://msdn.microsoft.com/library/dd642331(v=vs.110).aspx for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args) {
            if (!args.PrelaunchActivated) {
                await ActivationService.ActivateAsync(args);
            }

            FindConnection();
        }

        private void FindConnection() {
            cancelTokenSource = new CancellationTokenSource();
            cancelTokenSource.Token.Register(() => OnConnectionCancelled());

            Task<DeviceInformationCollection> task = UsbSerial.listAvailableDevicesAsync().AsTask<DeviceInformationCollection>(cancelTokenSource.Token);

            if (task != null) {
                //store the returned DeviceInformation items when the task completes
                task.ContinueWith(listTask => {
                    //store the result and populate the device list on the UI thread

                    var result = listTask.Result;
                    if (result != null && result.Count > 0) {
                        DeviceInformation device = result[0];
                        Connection = new UsbSerial(device);

                        MakeConnection();

                    }
                });
            }
        }

        private void MakeConnection() {
            Arduino = new RemoteDevice(App.Connection);
            Arduino.DeviceReady += OnDeviceReady;
            Arduino.DeviceConnectionFailed += OnConnectionFailed;

            connectionStopwatch.Reset();
            connectionStopwatch.Start();

            //App.Connection.begin(baudRate, SerialConfig.SERIAL_8N1);
        }

        protected override async void OnActivated(IActivatedEventArgs args) {
            await ActivationService.ActivateAsync(args);
        }

        private void OnDeviceReady() {

            

            
            
            //var balls = CoreDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
            //    timeout.Stop();
                //ConnectMessage.Text = "Successfully connected!";


            //    connectionStopwatch.Stop();

                //TrackConnectionEvent(ConnectMessage.Text, connectionStopwatch);

                //this.Frame.Navigate(typeof(MainPage));
            //}));
        }

        private void OnConnectionFailed(string message) {
            //var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
            //    timeout.Stop();
            //    ConnectMessage.Text = "Connection attempt failed: " + message;

            //    //telemetry
            //    connectionStopwatch.Stop();
            //    App.Telemetry.Context.Properties["connection.state"] = "Failed";
            //    TrackConnectionEvent(ConnectMessage.Text, connectionStopwatch);

            //    Reset();
            //}));
        }

        private void OnConnectionCancelled() {
            timeout.Stop();
            
            connectionStopwatch.Stop();
            

            Reset();
        }

        private void Reset() {
            if (App.Connection != null) {
                App.Connection.ConnectionEstablished -= OnDeviceReady;
                App.Connection.ConnectionFailed -= OnConnectionFailed;
                App.Connection.end();
            }

            if (cancelTokenSource != null) {
                cancelTokenSource.Dispose();
            }

            Connection = null;
            Arduino = null;
            cancelTokenSource = null;

        }

        private ActivationService CreateActivationService() {
            return new ActivationService(this, typeof(Views.PivotPage));
        }
    }
}
