using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoRIO.Bluetooth {
    public interface BTCallback {

        // Connection events
        void onDeviceDiscovered(string address, string name, int rssi);
        void onConnectToDevice(string address, string name, bool success);
        void onDisconnectFromDevice(string address, string name);

        // Data events
        void onUartDataReceived(byte[] data);
        void onUartDataSent(byte[] value, bool success);
        void onBluetoothPowerChanged(bool enabled);
    }
}
