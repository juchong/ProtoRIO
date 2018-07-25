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

        void OnConnectClicked(object src, EventArgs e) {
            Debug.WriteLine("ScanError: " + bluetooth.enumerateDevices());
        }

        public class MyBtCallback : BTCallback {

            public static MainPage mainPage;

            // Connection events
            public void onDeviceDiscovered(string address, string name, int rssi) {
                if(!mainPage.discoveredDevices.Contains(address)){
                    mainPage.discoveredDevices.Add(address);
                    if (name != null && name.Equals("RN_BLE")) {
                        Debug.WriteLine("Connecting to " + name + "...");
                        bluetooth.connect(address);
                        bluetooth.endEnumeration();
                    }
                }
            }
            public void onConnectToDevice(string address, string name, bool success) {
                bluetooth.subscribeToUartChars();
                Debug.WriteLine("Connected to " + name + ".");
            }
            public void onDisconnectFromDevice(string address, string name) {
                Debug.WriteLine("Disconnected from " + name + ".");
            }

            // Data events
            public void onUartDataReceived(byte[] data) {
                bluetooth.writeToUart(data);
            }
            public void onUartDataSent(byte[] value, bool success) {

            }
            public void onBluetoothPowerChanged(bool enabled) {
                Debug.WriteLine("Bluetooth Enabled: " + enabled);
            }
        }
    }
}
