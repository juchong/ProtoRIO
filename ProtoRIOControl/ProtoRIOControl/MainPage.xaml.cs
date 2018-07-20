using ProtoRIO.Bluetooth;
using ProtoRIOControl.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ProtoRIOControl {
    public partial class MainPage : ContentPage, BLEDelegate{

        BLEClient client;

        public MainPage() {
            InitializeComponent();

            if (!DesignMode.IsDesignModeEnabled)
                client = BLEClient.Builder.Create(this);
        }

        #region BLEDelegate
        public void OnBluetoothPowerChanged(bool enabled) {

        }

        public void OnCharacteristicRead(string characteristic, bool success, byte[] value) {

        }

        public void OnCharacteristicWrite(string characteristic, bool success, byte[] value) {

        }

        public void OnConnectToDevice(string address, string name, bool success) {

        }

        public void OnDescriptorRead(string descriptor, bool success, byte[] value) {

        }

        public void OnDescriptorWrite(string descriptor, bool success, byte[] value) {

        }

        public void OnDeviceDiscovered(string address, string name, int rssi) {

        }

        public void OnDisconnectFromDevice(string address, string name) {

        }

        public void OnServicesDiscovered() {

        }
        #endregion
    }
}
