using Acr.UserDialogs;
using ProtoRIO.Bluetooth;
using ProtoRIOControl.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

using Timer = System.Timers.Timer;

namespace ProtoRIOControl {
    public partial class MainPage : TabbedPage {
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

        public static Timer readRequestTimer;

        public MainPage() {
            InitializeComponent();
            if(instance == null)
                instance = this;
            if (readRequestTimer == null) {
                readRequestTimer = new Timer(250); // Request a read every 250ms
                readRequestTimer.AutoReset = true;
                readRequestTimer.Elapsed += RequestRead;
            }
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



        #region Bluetooth Write Functions

        /// <summary>
        /// Set the value of PWM A
        /// </summary>
        /// <param name="position">A percentage (-100 to 100) or a servo angle (0 to 180)</param>
        /// <param name="isServo">Is this server data (an angle not a percent)</param>
        public static void sendPWMA(double position) {
            // Convert from the -100 to 100 range of the slider into the 0 to 200 range used by the ProtoRIO
            position += 100;
            bluetooth.writeToUart(Encoding.ASCII.GetBytes(OutData.sendPWMA + position + '\n'));
        }

        /// <summary>
        /// Set the value of PWM B
        /// </summary>
        /// <param name="position">A percentage (-100 to 100) or a servo angle (0 to 180)</param>
        /// <param name="isServo">Is this server data (an angle not a percent)</param>
        public static void sendPWMB(double position) {
            // Convert from the -100 to 100 range of the slider into the 0 to 200 range used by the ProtoRIO
            position += 100;
            bluetooth.writeToUart(Encoding.ASCII.GetBytes(OutData.sendPWMB + position + '\n'));
        }

        /// <summary>
        /// Set which solenoids are on
        /// </summary>
        /// <param name="aOn">Set soleoid a on</param>
        /// <param name="bOn">Set solenoid b on</param>
        public static void sendSolenoid(bool aOn, bool bOn) {
            int state = OutData.bothSolenoidsOff;
            if (aOn && bOn)
                state = OutData.bothSolenoidsOn;
            else if (aOn)
                state = OutData.solenoidAOn;
            else if (bOn)
                state = OutData.solenoidBOn;
            bluetooth.writeToUart(Encoding.ASCII.GetBytes(OutData.sendSolenoid + state + '\n'));
        }

        /// <summary>
        /// Select a sensor for sensor A
        /// </summary>
        /// <param name="sensorType">The sensor type</param>
        public static void sendSensorASelection(int sensorType) {
            bluetooth.writeToUart(Encoding.ASCII.GetBytes(OutData.sendSensorA + sensorType + '\n'));
        }

        /// <summary>
        /// Select a sensor for sensor B
        /// </summary>
        /// <param name="sensorType">The sensor type</param>
        public static void sendSensorBSelection(int sensorType) {
            bluetooth.writeToUart(Encoding.ASCII.GetBytes(OutData.sendSensorB + sensorType + '\n'));
        }

        /// <summary>
        /// Send the user configured setting for sensor A
        /// </summary>
        /// <param name="setting">The setting</param>
        public static void sendSensorASetting(int setting) {
            bluetooth.writeToUart(Encoding.ASCII.GetBytes(OutData.sendSensorASetting + setting + '\n'));
        }

        /// <summary>
        /// Send the user configured setting for sensor B
        /// </summary>
        /// <param name="setting">The setting</param>
        public static void sendSensorBSetting(int setting) {
            bluetooth.writeToUart(Encoding.ASCII.GetBytes(OutData.sendSensorBSetting + setting + '\n'));
        }

        /// <summary>
        /// Request that the ProtRIO send sensor information
        /// </summary>
        /// <param name="sender">The source timer</param>
        /// <param name="e">Event args</param>
        public static void RequestRead(object sender, System.Timers.ElapsedEventArgs e) {
            if (bluetooth.isConnected()) {
                bluetooth.writeToUart(Encoding.ASCII.GetBytes(OutData.requestRead + '\n'));
            }
        }

        #endregion



        #region Bluetooth Read Processing

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
                instance.sensorsPage.setSensorAInfo(sensorARes1, sensorARes2);
                instance.sensorsPage.setSensorBInfo(sensorBRes1, sensorBRes2);
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
            if (source == null || startText == null || endText == null)
                return null;
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
            if (source == null || tag == null)
                return null;
            int index = source.IndexOf(tag, StringComparison.CurrentCulture);
            if (index < 0)
                return null;
            return source.Substring(index + tag.Length);
        }

        #endregion



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
                        Device.BeginInvokeOnMainThread(() => {
                            instance.statusPage.setStatusLabel(AppResources.StatusConnected + name, true);
                            instance.pwmPage.enableAll();
                            instance.sensorsPage.enableAll();
                            instance.pneumaticsPage.enableSolenoids();
                        });
                        readRequestTimer.Start(); // Start polling for data
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
                readRequestTimer.Stop();
                Device.BeginInvokeOnMainThread(() => { 
                    if (!manualDisconnect)
                        UserDialogs.Instance.Alert(AppResources.AlertLostConnectionMessage, AppResources.AlertLostConnectionTitle, AppResources.AlertOk);
                    instance.statusPage.setStatusLabel(AppResources.StatusNotConnected, false);
                    instance.pwmPage.disableAll();
                    instance.sensorsPage.disableAll();
                    instance.pneumaticsPage.disableSolenoids();
                    manualDisconnect = false;
                });
            }

            // Data events
            public void onUartDataReceived(byte[] data) {
                foreach(byte b  in data){
                    readBuffer += (char)b;
                    if(readBuffer.EndsWith("\n", StringComparison.CurrentCulture)){
                        Debug.WriteLine("Got Complete data: " + readBuffer.Length);
                        processData(String.Copy(readBuffer)); // DO NOT AWAIT THIS OR DATA MAY BE MISSED!!!
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
