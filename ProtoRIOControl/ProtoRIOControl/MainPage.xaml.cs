using ProtoRIO.Bluetooth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ProtoRIOControl {
    public partial class MainPage : ContentPage, BLEDelegate {
        BLEClient client;
        public MainPage() {
            InitializeComponent();
            client = BLEClient.Create(this);
        }

        void onScanClicked() {
            Debug.WriteLine("Error: " + client.ScanForDevices().ToString());
        }

        #region BLEDelegate
        // Connection events
        public void OnDeviceDiscovered(string address, string name, int rssi) {
            Debug.WriteLine("Found Device with address: " + address);
        }
        public void OnConnectToDevice(string address, string name, bool success) {

        }
        public void OnDisconnectFromDevice(string address, string name) {

        }
        public void OnServicesDiscovered() {

        }

        // Data events
        public void OnCharacteristicRead(string characteristic, bool success, byte[] value) {

        }
        public void OnCharacteristicWrite(string characteristic, bool success, byte[] value) {

        }
        public void OnDescriptorRead(string descriptor, bool success, byte[] value) {

        }
        public void OnDescriptorWrite(string descriptor, bool success, byte[] value) {

        }
        public void OnBluetoothPowerChanged(bool enabled) {

        }
        #endregion
    }
}
