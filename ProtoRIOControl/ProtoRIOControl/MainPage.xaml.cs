using ProtoRIO.Bluetooth;
using ProtoRIOControl.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ProtoRIOControl {
    public partial class MainPage : TabbedPage{
        public static MainPage instance;

        public static IBluetooth bluetooth;
        public static MyBtCallback btCallback = new MyBtCallback();

        public static List<string> discoveredDevices = new List<string>();
        public static ObservableCollection<string> deviceNames = new ObservableCollection<string>();
        public static BluetoothDevicePage bluetoothDevicePage;
        public static string connectedDeviceName = AppResources.UnknownDevice;
        public static int requestTime = -1;

        public MainPage() {
            InitializeComponent();
            instance = this;
        }

        protected override void OnDisappearing() {
            base.OnDisappearing();
            bluetooth.endEnumeration();
            bluetooth.disconnect();
            statusPage.setStatusLabel(AppResources.StatusNotConnected, Color.Red);
        }

        void OnConnectClicked(object src, EventArgs e) {
            bluetooth.endEnumeration();
            bluetooth.disconnect();
            BtError error = bluetooth.checkBtSupport();
            switch (error) {
                case BtError.Disabled:
                    bluetooth.showEnableBtPrompt(AppResources.EnableBTTitle, AppResources.EnableBTMessage, AppResources.EnableBTConfirm, AppResources.EnableBTCancel);
                    requestTime = Environment.TickCount;
                    break;
                case BtError.NoBLE:
                case BtError.NoBluetooth:
                case BtError.Unknown:
                case BtError.UnsupportedPlatform:
                default:
                    DisplayAlert(AppResources.AlertBtErrorTitle, AppResources.AlertBtErrorMessage, AppResources.AlertOk);
                    break;
                case BtError.None:
                    discoveredDevices.Clear();
                    deviceNames.Clear();
                    bluetoothDevicePage = new BluetoothDevicePage();
                    Navigation.PushModalAsync(bluetoothDevicePage);
                    break;
            }
        }

        protected override void OnAppearing() {
            // Just returned from some other page that no longer exists
            bluetoothDevicePage = null;
        }

        public class MyBtCallback : BTCallback {

            // Connection events
            public void onDeviceDiscovered(string address, string name, int rssi) {
                Debug.WriteLine("GOT A DEVICE!!!");
                if (!discoveredDevices.Contains(address)){
                    discoveredDevices.Add(address);
                    deviceNames.Add(name == null ? AppResources.UnknownDevice : name);
                }
            }
            public void onConnectToDevice(string address, string name, bool success) {
                if (bluetooth.hasUartService()) {
                    bluetooth.subscribeToUartChars();
                    connectedDeviceName = name;
                    instance.statusPage.setStatusLabel(AppResources.StatusConnected + name, Color.Green);
                } else {
                    bluetooth.disconnect();
                    instance.DisplayAlert(AppResources.AlertInvalidDeviceTitle, AppResources.AlertInvalidDeviceMessage, AppResources.AlertOk);
                }
            }
            public void onDisconnectFromDevice(string address, string name) {
                instance.DisplayAlert(AppResources.AlertLostConnectionTitle, AppResources.AlertLostConnectionMessage, AppResources.AlertOk);
                instance.statusPage.setStatusLabel(AppResources.StatusNotConnected, Color.Red);
            }

            // Data events
            public void onUartDataReceived(byte[] data) {
                
            }
            public void onUartDataSent(byte[] value) {
            
            }
            public void onBluetoothPowerChanged(bool enabled) {
                if (enabled && Environment.TickCount - requestTime <= 10000) {
                    // Auto retry if bt is enabled within 10 seconds
                    instance.OnConnectClicked(null, null);
                }
            }
        }
    }
}
