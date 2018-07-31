using System;
using System.Threading;
using ProtoRIO.Bluetooth;

namespace ProtoRIOControl {
    public class BtManager: BTCallback {
        public IBluetooth bluetooth;

        public void onBluetoothPowerChanged(bool enabled) {
            throw new NotImplementedException();
        }

        public void onConnectToDevice(string address, string name, bool success) {
            throw new NotImplementedException();
        }

        public void onDeviceDiscovered(string address, string name, int rssi) {
            throw new NotImplementedException();
        }

        public void onDisconnectFromDevice(string address, string name) {
            throw new NotImplementedException();
        }

        public void onUartDataReceived(byte[] data) {
            throw new NotImplementedException();
        }

        public void onUartDataSent(byte[] value) {
            throw new NotImplementedException();
        }
    }
}
