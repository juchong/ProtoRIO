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
        public static string readBuffer = "";

        private static IProgressDialog connectProgressDialog;

        public MainPage() {
            InitializeComponent();
            instance = this;
        }

        /// <summary>
        /// Called when starting the app (and loading this page) or when returning to this page from another page
        /// </summary>
        protected override void OnAppearing() {
            // Just returned from some other page that no longer exists
            if (bluetoothDevicePage != null) {
                bluetoothDevicePage = null;
                bluetooth.endEnumeration();
                if (deviceToConnectTo > -1) {
                    if (deviceToConnectTo >= discoveredDevices.Count) {
                        Debug.WriteLine("An invalid device was selected. Index was " + deviceToConnectTo + " but there were only " + discoveredDevices.Count + " devices.");
                        return;
                    }
                    bluetooth.connect(discoveredDevices[deviceToConnectTo]);
                    connectProgressDialog = UserDialogs.Instance.Progress(
                        new ProgressDialogConfig() {
                            Title = AppResources.Connecting + deviceNames[deviceToConnectTo],
                            IsDeterministic = false,
                            OnCancel = () => {
                                bluetooth.cancelConnect();
                                bluetooth.disconnect();
                            },
                            CancelText = AppResources.Cancel
                        }
                    );
                    connectProgressDialog.Show();
                    connectedDeviceName = deviceNames[deviceToConnectTo];
                    deviceToConnectTo = -1;
                }
            }
        }

        /// <summary>
        /// Handle the connect button. If connected disconnect. Otherwise connect.
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="e">EventArgs</param>
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

        /// <summary>
        /// Handle data sent by the ProtoRIO
        /// </summary>
        /// <param name="data">The data read from the "UART" port</param>
        public static async Task processData(string data){
            await Task.Yield();
            // Data will be sent all in one string with a format similar to the following
            // RBV[VOLTAGE]RBI[CURRENT]XR1A[SENSORA1]R2A[SENSORA2]YR1B[SENSORB1]R2B[SENSORB2]Z\n
            // Parse the string
            Debug.WriteLine("Process Data: " + data.Length);
            string batteryVoltage = getValue(substring(data, InData.batteryVIn, InData.batteryIIn), InData.batteryVIn);
            string batteryCurrent = getValue(substring(data, InData.batteryIIn, InData.batteryEnd), InData.batteryIIn);
            string sensorARes1 = getValue(substring(data, InData.sensorARes1, InData.sensorARes2), InData.sensorARes1);
            string sensorARes2 = getValue(substring(data, InData.sensorARes2, InData.sensorAEnd), InData.sensorARes2);
            string sensorBRes1 = getValue(substring(data, InData.sensorBRes1, InData.sensorBRes2), InData.sensorBRes1);
            string sensorBRes2 = getValue(substring(data, InData.sensorBRes2, InData.sensorBEnd), InData.sensorBRes2);
            Device.BeginInvokeOnMainThread(() => {
                instance.statusPage.setBatteryInfo(batteryVoltage, batteryCurrent);
            });
        }

        /// <summary>
        /// Get a substring based on the text in a string
        /// </summary>
        /// <returns>The substring or null.</returns>
        /// <param name="source">The source string</param>
        /// <param name="startText">The text to start at.</param>
        /// <param name="endText">The text to end at.</param>
        public static string substring(string source, string startText, string endText) {
            int start = source.IndexOf(startText, StringComparison.CurrentCulture);
            int end = source.IndexOf(endText, StringComparison.CurrentCulture);
            if(start < 0 || start > end || end < 0){
                return null;
            }
            return source.Substring(start, end - start);
        }

        /// <summary>
        /// Get the value after a specified tag in a string.
        /// </summary>
        /// <returns>The value or null</returns>
        /// <param name="source">The source string</param>
        /// <param name="tag">The tag</param>
        public static string getValue(string source, string tag){
            int index = source.IndexOf(tag, StringComparison.CurrentCulture);
            if (index < 0)
                return null;
            return source.Substring(index + tag.Length);
        }

        /// <summary>
        /// A callback to be used with the app's IBluetooth object
        /// </summary>
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
                        Device.BeginInvokeOnMainThread(() => instance.statusPage.setStatusLabel(AppResources.StatusConnected + name, true));
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
                    instance.statusPage.setStatusLabel(AppResources.StatusNotConnected, false);
                    manualDisconnect = false;
                });
            }

            // Data events
            public void onUartDataReceived(byte[] data) {
                foreach(byte b  in data){
                    readBuffer += (char)b;
                    if(readBuffer.EndsWith("\n", StringComparison.CurrentCulture)){
                        Debug.WriteLine("Got Complete data: " + readBuffer.Length);
                        processData(String.Copy(readBuffer)); // DO NOT AWAIT THIS OR DATA MAY BE LOST!!!
                        readBuffer = "";
                    }
                }
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
