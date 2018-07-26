using ProtoRIO.Bluetooth;
using ProtoRIOControl.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ProtoRIOControl {
    public partial class MainPage : TabbedPage{

        public static IBluetooth bluetooth;
        public static MyBtCallback btCallback = new MyBtCallback();

        List<string> discoveredDevices = new List<string>();

        public MainPage() {
            InitializeComponent();
            MyBtCallback.mainPage = this;
        }

        protected override void OnDisappearing() {
            base.OnDisappearing();
            bluetooth.endEnumeration();
            bluetooth.disconnect();
        }

        void OnConnectClicked(object src, EventArgs e) {
            BtError error = bluetooth.enumerateDevices();
            Debug.WriteLine("ScanError: " + error);
            if(error == BtError.Disabled){
                bluetooth.showEnableBtPrompt(AppResources.EnableBTTitle, AppResources.EnableBTMessage, AppResources.EnableBTConfirm, AppResources.EnableBTCancel);
                Debug.WriteLine("Done showing prompt");
            }
        }

        public class MyBtCallback : BTCallback {

            public static MainPage mainPage;

            // Connection events
            public void onDeviceDiscovered(string address, string name, int rssi) {
                if(!mainPage.discoveredDevices.Contains(address)){
                    Debug.WriteLine("Have name of discovered device: " + (name != null)); //TODO: Fix this!!!
                    mainPage.discoveredDevices.Add(address);
                    if (name != null && name.Equals("RN_BLE")) {
                        Debug.WriteLine("Connecting to " + name + "...");
                        bluetooth.connect(address);
                        bluetooth.endEnumeration();
                    }
                }
            }
            public void onConnectToDevice(string address, string name, bool success) {
                Debug.WriteLine("Connected to " + name + ".");

                if (bluetooth.hasUartService()) {
                    bluetooth.subscribeToUartChars();
                } else {
                    bluetooth.disconnect();
                    Debug.WriteLine("Disconnecting as no UART service was found.");
                }
            }
            public void onDisconnectFromDevice(string address, string name) {
                Debug.WriteLine("Disconnected from " + name + ".");
            }

            // Data events
            public void onUartDataReceived(byte[] data) {
                bluetooth.writeToUart(data);
            }
            public void onUartDataSent(byte[] value) {
            
            }
            public void onBluetoothPowerChanged(bool enabled) {
                Debug.WriteLine("Bluetooth Enabled: " + enabled);
            }
        }
    }
}
