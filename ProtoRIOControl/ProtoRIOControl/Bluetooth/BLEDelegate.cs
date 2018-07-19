using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoRIO.Bluetooth {
    public interface BLEDelegate {

        // Connection events
        void OnDeviceDiscovered(string address, string name, int rssi);
        void OnConnectToDevice(string address, string name, bool success);
        void OnDisconnectFromDevice(string address, string name);
        void OnServicesDiscovered();

        // Data events
        void OnCharacteristicRead(string characteristic, bool success, byte[] value);
        void OnCharacteristicWrite(string characteristic, bool success, byte[] value);
        void OnDescriptorRead(string descriptor, bool success, byte[] value);
        void OnDescriptorWrite(string descriptor, bool success, byte[] value);
        void OnBluetoothPowerChanged(bool enabled);
    }
}
