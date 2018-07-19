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
        protected List<string> Services { get; set; }
        protected List<string> Characteristics { get; set; }
        protected List<string> Descriptors { get; set; }
        protected List<string> ScanServices { get; set; }
        protected bool IsScanning { get; set; }
        protected bool IsConnected { get; set; }
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

        /// <summary>
        /// Create the apropriate BLEClient based on the targeted platform
        /// </summary>
        public static BLEClient Create() {
#if __ANDROID__
            return new AndroidBLEClient();
#elif __IOS__
            return new IOSBLEClient();
#else // UWP
            return new UWPBLEClient();
#endif
        }
    }
}
