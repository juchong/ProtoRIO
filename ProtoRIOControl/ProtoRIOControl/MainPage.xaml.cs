using Acr.UserDialogs;
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

        public static bool manualDisconnect = false;

        public static int deviceToConnectTo = -1;

        private static IProgressDialog connectProgressDialog;

        public MainPage() {
            InitializeComponent();
            instance = this;
        }

        protected override void OnDisappearing() {
            base.OnDisappearing();
        }

        void OnConnectClicked(object src, EventArgs e) {
            Debug.WriteLine("IsConnected: " + bluetooth.isConnected());
            if(!bluetooth.isConnected()){
                bluetooth.endEnumeration();
                bluetooth.disconnect();
                BtError error = bluetooth.checkBtSupport();
                switch (error) {
                    case BtError.Disabled:
                        bluetooth.showEnableBtPrompt(AppResources.EnableBTTitle, AppResources.EnableBTMessage, AppResources.EnableBTConfirm, AppResources.EnableBTCancel);
                        requestTime = Environment.TickCount;
                        break;
                    case BtError.None:
                        // Do not allow this to be shown more than once at a time
                        if (bluetoothDevicePage == null) {
                            discoveredDevices.Clear();
                            deviceNames.Clear();
                            bluetooth.enumerateDevices();
                            bluetoothDevicePage = new BluetoothDevicePage();
                            Navigation.PushModalAsync(bluetoothDevicePage);
                        }
                        break;
                    default:
                        UserDialogs.Instance.Alert(AppResources.AlertBtErrorMessage, AppResources.AlertBtErrorTitle, AppResources.AlertOk);
                        break;
                }
            }else{
                UserDialogs.Instance.Confirm(new ConfirmConfig() {
                    Title = AppResources.ConfirmDisconnectTitle,
                    Message = AppResources.ConfirmDisconnectMessage.Replace("%DEV%", connectedDeviceName),
                    OkText = AppResources.Yes,
                    CancelText = AppResources.No,
                    OnAction = (result) => {
                        if (result) {
                            bluetooth.endEnumeration();
                            manualDisconnect = true;
                            bluetooth.disconnect();
                        }
                    }
                });
            }
        }

        protected override void OnAppearing() {
            // Just returned from some other page that no longer exists
            if(bluetoothDevicePage != null){
                bluetoothDevicePage = null;
                bluetooth.endEnumeration();
                if(deviceToConnectTo > -1){
                    if(deviceToConnectTo >= discoveredDevices.Count){
                        Debug.WriteLine("An invalid device was selected. Index was " + deviceToConnectTo + " but there were only " + discoveredDevices.Count + " devices.");
                        return;
                    }
                    bluetooth.connect(discoveredDevices[deviceToConnectTo]);
                    connectProgressDialog = UserDialogs.Instance.Progress(
                        new ProgressDialogConfig() {
                            Title = AppResources.Connecting + deviceNames[deviceToConnectTo],
                            IsDeterministic = false,
                            OnCancel = () => {
                                Device.BeginInvokeOnMainThread(() => connectProgressDialog.Show()); // Do not allow cancel!!! The bt connectino will time out
                            },
                            CancelText = ""
                        }
                    );
                    connectProgressDialog.Show();
                    connectedDeviceName = deviceNames[deviceToConnectTo];
                    deviceToConnectTo = -1;
                }
            }
        }

        public class MyBtCallback : BTCallback {

            // Connection events
            public void onDeviceDiscovered(string address, string name, int rssi) {
                if (!discoveredDevices.Contains(address)){
                    discoveredDevices.Add(address);
                    deviceNames.Add(name == null ? AppResources.UnknownDevice : name);
                }
            }
            public void onConnectToDevice(string address, string name, bool success) {
                Device.BeginInvokeOnMainThread(() => {
                    connectProgressDialog.Hide();
                    connectProgressDialog = null;
                });
                if(success){
                    if (bluetooth.hasUartService()) {
                        bluetooth.subscribeToUartChars();
                        connectedDeviceName = name;
                        Device.BeginInvokeOnMainThread(() => instance.statusPage.setStatusLabel(AppResources.StatusConnected + name, Color.Green));
                    } else {
                        manualDisconnect = true;
                        bluetooth.disconnect();
                        Device.BeginInvokeOnMainThread(() => UserDialogs.Instance.Alert(AppResources.AlertInvalidDeviceMessage, AppResources.AlertInvalidDeviceTitle, AppResources.AlertOk));
                    }
                }else{
                    Device.BeginInvokeOnMainThread(() => UserDialogs.Instance.Alert(AppResources.AlertConnectFailMessage, AppResources.AlertConnectFailTitle + connectedDeviceName, AppResources.AlertOk));
                }
            }
            public void onDisconnectFromDevice(string address, string name) {
                Device.BeginInvokeOnMainThread(() => { 
                    if (!manualDisconnect)
                        UserDialogs.Instance.Alert(AppResources.AlertLostConnectionMessage, AppResources.AlertLostConnectionTitle, AppResources.AlertOk);
                    instance.statusPage.setStatusLabel(AppResources.StatusNotConnected, Color.Red);
                    manualDisconnect = false;
                });
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
