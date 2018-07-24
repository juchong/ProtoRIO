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
    public partial class MainPage : TabbedPage, BLEDelegate{

        static BLEClient client;
        const string uartService = "49535343-FE7D-4AE5-8FA9-9FAFD205E455";
        const string txCharacteristic = "49535343-1E4D-4BD9-BA61-23C647249616";
        const string rxCharacteristic = "49535343-8841-43F4-A8D4-ECBE34729BB3";

        public MainPage() {
            InitializeComponent();

            if (!DesignMode.IsDesignModeEnabled && client == null)
                client = BLEClient.Builder.Create(this);
        }

        void OnConnectClicked(object src, EventArgs e) {
            Debug.WriteLine("ScanError: " + client.ScanForDevices());
        }

        #region BLEDelegate
        public void OnBluetoothPowerChanged(bool enabled) {
            Debug.WriteLine("BT Status: " + enabled);
        }

        public void OnCharacteristicRead(string characteristic, bool success, byte[] value) {
            client.WriteCharacteristic(rxCharacteristic, value); // This will echo whatever is sent. Easy way to test TX and RX
        }

        public void OnCharacteristicWrite(string characteristic, bool success, byte[] value) {

        }

        public void OnConnectToDevice(string address, string name, bool success) {
            Debug.WriteLine("Connected to " + name + ".");
        }

        public void OnDescriptorRead(string descriptor, bool success, byte[] value) {

        }

        public void OnDescriptorWrite(string descriptor, bool success, byte[] value) {

        }

        public void OnDeviceDiscovered(string address, string name, int rssi) {
            if(name != null && name.Equals("RN_BLE")){
                Debug.WriteLine("Found BT Module. Connecting...");
                Debug.WriteLine("Address: " + address);
                client.ConnectToDevice(address);
                client.StopScanning();
            }
        }

        public void OnDisconnectFromDevice(string address, string name) {
            Debug.WriteLine("Disconnected from " + name + ".");
        }

        public void OnServicesDiscovered() {
            Debug.WriteLine("Discovered Services");
            if (client.HasService(uartService)){
                Debug.WriteLine("Has Transparent UART Service");
                client.SubscribeToCharacteristic(txCharacteristic);
            }else{
                Debug.WriteLine("No Transparent UART Service found!!!");
                Debug.WriteLine("Disconnecting");
            }
        }
        #endregion
    }
}
