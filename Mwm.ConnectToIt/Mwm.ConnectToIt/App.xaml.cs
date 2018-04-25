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
using Windows.ApplicationModel.Core;
using Microsoft.Maker.Firmata;

namespace Mwm.ConnectToIt {
    public sealed partial class App : Application {
        private Lazy<ActivationService> _activationService;

        public static IStream Connection {
            get;
            set;
        }

        public static UwpFirmata Firmata {
            get; set;
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
        }

        

        protected override async void OnActivated(IActivatedEventArgs args) {
            await ActivationService.ActivateAsync(args);
        }

        private ActivationService CreateActivationService() {
            return new ActivationService(this, typeof(Views.PivotPage));
        }
    }
}
