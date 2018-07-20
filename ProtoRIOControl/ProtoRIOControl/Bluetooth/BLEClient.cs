using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoRIO.Bluetooth {

    #region Types
    public enum BtError { None, NoBluetooth, NoBLE, Disabled, NoServer, AlreadyRunning, Unknown, UnsupportedPlatform }
    public enum AdvertiseError { None, DataTooLarge, TooManyAdvertisers, AlreadyStarted, Unknown }
    public enum CharProperties { Broadcast = 1, Read = 2, WriteNoResponse = 4, Write = 8, Notify = 16, Indicate = 32, SignedWrite = 64, Extended = 128 }
    public enum CharPermissions { Read = 1, ReadEncrypted = 2, Write = 4, WriteEncrypted = 8 }
    #endregion

    /// <summary>
    /// This is a generic interface(abstract class) defining the functions and properties needed for a BLE Client
    /// This class is also used to create a platform specific BLEClient based on the current platform
    /// </summary>
    public abstract class BLEClient {

        #region Properties
        protected BLEDelegate Delegate { get; set; }
        protected List<string> Services = new List<string>();
        protected List<string> Characteristics = new List<string>();
        protected List<string> Descriptors = new List<string>();
        protected List<string> ScanServices = new List<string>();
        protected bool IsScanning = false;
        protected bool IsConnected = false;
        #endregion
        #region Client Control
        public abstract void ScanForService(string service, bool scanFor = true);
        public abstract BtError CheckBluetooth();
        public abstract void RequestEnableBt();
        public abstract BtError ScanForDevices();
        public abstract void StopScanning();
        public abstract void ConnectToDevice(string deviceAddress);
        public abstract void Disconnect();
        public abstract void SubscribeToCharacteristic(string characteristic, bool subscribe = true);
        #endregion
        #region Characteristics and Descriptors
        public abstract void WriteCharacteristic(string characteristic, byte[] data);
        public abstract void ReadCharacteristic(string characteristic);
        public abstract void WriteDescriptor(string descriptor, byte[] data);
        public abstract void ReadDescriptor(string descriptor);
        public abstract bool HasService(string service);
        public abstract bool HasCharacteristic(string characteristic);
        public abstract bool HasDescriptor(string descriptor);
        #endregion

        public static BLEClientBuilder Builder;
    }
    public interface BLEClientBuilder {
        BLEClient Create(BLEDelegate bleDelegate);
    }
}
