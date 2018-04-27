using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;

namespace Mwm.ConnectToIt.Views {
    public sealed partial class MainPage : Page, INotifyPropertyChanged {

        const byte ECHO_COMMAND = 0x42;

        DispatcherTimer timeout;
        // stopwatch for tracking connection timing
        Stopwatch connectionStopwatch = new Stopwatch();
        CancellationTokenSource cancelTokenSource;

        public MainPage() {
            InitializeComponent();


            //var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
            //    FindConnection();
            //});
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null) {
            if (Equals(storage, value)) {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        //protected override void OnNavigatedTo(NavigationEventArgs e) {
        //    base.OnNavigatedTo(e);

        //    FindConnection();
        //}
        
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


                        App.Connection = new UsbSerial(device);

                        MakeConnection();
                    }
                });
            }
        }

        private void MakeConnection() {

            App.Firmata = new Microsoft.Maker.Firmata.UwpFirmata();
            //App.Arduino = new RemoteDevice(App.Connection);
            App.Arduino = new RemoteDevice(App.Firmata);

            App.Arduino.DeviceReady += OnDeviceReady;
            App.Arduino.DeviceConnectionFailed += OnConnectionFailed;
            App.Arduino.DeviceConnectionLost += OnConnectionLost;
            App.Arduino.StringMessageReceived += OnStringMessageReceived;
            App.Arduino.SysexMessageReceived += OnSysexMessageReceived;

            connectionStopwatch.Reset();
            connectionStopwatch.Start();

            App.Firmata.begin(App.Connection);
            App.Connection.begin(57600, SerialConfig.SERIAL_8N1);

            //timeout = new DispatcherTimer();
            //timeout.Interval = new TimeSpan(0, 0, 30);
            //timeout.Tick += Connection_TimeOut;
            //timeout.Start();
        }

        private void OnSysexMessageReceived(byte command, DataReader message) {
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
                //timeout.Stop();
                ConnectMessage.Text = $"Sysex Message Received: {message.ToString()}!";
                connectionStopwatch.Stop();
                Reset();
            }));
        }

        private void OnStringMessageReceived(string message) {
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
                ReturnMessage.Text = ReturnMessage.Text + message + Environment.NewLine;
            }));
        }

        private void OnConnectionLost(string message) {
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
                //timeout.Stop();
                ConnectMessage.Text = "Connection Lost!";
                connectionStopwatch.Stop();
                Reset();
            }));
        }

        private void OnDeviceReady() {
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
                //timeout.Stop();
                ConnectMessage.Text = "Successfully connected!";
                connectionStopwatch.Stop();
                //this.Frame.Navigate(typeof(MainPage));
            }));
        }

        private void OnConnectionFailed(string message) {
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
                //timeout.Stop();
                ConnectMessage.Text = "Connection attempt failed: " + message;
                connectionStopwatch.Stop();
                Reset();
            }));
        }

        private void OnConnectionCancelled() {
            //timeout.Stop();
            ConnectMessage.Text = "Connection Cancelled.";
            connectionStopwatch.Stop();
            Reset();
        }

        private void Connection_TimeOut(object sender, object e) {
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
                //timeout.Stop();
                ConnectMessage.Text = "Connection attempt timed out.";
                connectionStopwatch.Stop();
                Reset();
            }));
        }

        private void Reset() {
            if (App.Connection != null) {
                App.Connection.ConnectionEstablished -= OnDeviceReady;
                App.Connection.ConnectionFailed -= OnConnectionFailed;
                App.Connection.end();
                ConnectMessage.Text = "Disconnected.";
            }

            if (cancelTokenSource != null) {
                cancelTokenSource.Dispose();
            }

            App.Connection = null;
            App.Firmata = null;
            App.Arduino = null;
            cancelTokenSource = null;

        }

        private void BlinkLight_Click(object sender, RoutedEventArgs e) {
            var state = App.Arduino.digitalRead(13);
            var nextState = (state == PinState.HIGH) ? PinState.LOW : PinState.HIGH;

            App.Arduino.digitalWrite(13, nextState);
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e) {
            //var buffer = CryptographicBuffer.ConvertStringToBinary(SourceMessage.Text, BinaryStringEncoding.Utf8);
            //var buffer = Encoding.ASCII.GetBytes(SourceMessage.Text);
            //App.Firmata.sendSysex(ECHO_COMMAND, buffer);

            App.Firmata.sendString(SourceMessage.Text);
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e) {
            FindConnection();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e) {
            Reset();
        }
    }
}
