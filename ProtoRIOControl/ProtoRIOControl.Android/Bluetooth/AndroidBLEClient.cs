using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ProtoRIO.Bluetooth;

namespace ProtoRIOControl.Droid.Bluetooth {
    class AndroidBLEClient : BLEClient {

        // Platform Specific Objects
        private List<BluetoothGattService> serviceObjects = new List<BluetoothGattService>();
        private List<BluetoothGattCharacteristic> characteristicObjects = new List<BluetoothGattCharacteristic>();
        private List<BluetoothGattDescriptor> descriptorObjects = new List<BluetoothGattDescriptor>();

        // Keep track of detected devices
        private List<string> deviceAddresses = new List<string>();
        private List<BluetoothDevice> devices = new List<BluetoothDevice>();

        // Android Specific Bluetooth Objects
        private Handler mainThread = new Handler(Looper.MainLooper);
        private BluetoothManager btManager;
        private BluetoothAdapter btAdapter;
        private BluetoothGatt gattConnection = null;

        // Enable BT Dialog Text
        string REQUEST_BT_TITLE = "Bluetooth Required";
        string REQUEST_BT_MESSAGE = "Enable bluetooth?";
        string REQUEST_BT_CONFIRM = "Yes";
        string REQUEST_BT_DENY = "No";

        public AndroidBLEClient(BLEDelegate bleDelegate) {
            this.Delegate = bleDelegate;
            btManager = (BluetoothManager)MainActivity.MainContext.GetSystemService(Context.BluetoothService);
            btAdapter = btManager.Adapter;
            // Watch for Bluetooth Power Changes
            new Thread(new ThreadStart(() => {
                bool lastState = false;
                while (true) {
                    bool state = btAdapter.IsEnabled;
                    if (state != lastState) {
                        lastState = state;
                        if (!state) {
                            StopScanning();
                            Disconnect();
                        }
                        mainThread.Post(() => {
                            Delegate.OnBluetoothPowerChanged(state);
                        });
                    }
                    Thread.Sleep(100);
                }
            }));
        }

        public override BtError CheckBluetooth() {
            if (!MainActivity.MainContext.PackageManager.HasSystemFeature(PackageManager.FeatureBluetooth))
                return BtError.NoBluetooth;
            if (!MainActivity.MainContext.PackageManager.HasSystemFeature(PackageManager.FeatureBluetoothLe))
                return BtError.NoBLE;
            if (btAdapter == null || !btAdapter.IsEnabled)
                return BtError.Disabled;
            return BtError.None;
        }

        public override void ConnectToDevice(string deviceAddress) {
            throw new NotImplementedException();
        }

        public override void Disconnect() {
            throw new NotImplementedException();
        }

        public override bool HasCharacteristic(string characteristic) {
            throw new NotImplementedException();
        }

        public override bool HasDescriptor(string descriptor) {
            throw new NotImplementedException();
        }

        public override bool HasService(string service) {
            throw new NotImplementedException();
        }

        public override void ReadCharacteristic(string characteristic) {
            throw new NotImplementedException();
        }

        public override void ReadDescriptor(string descriptor) {
            throw new NotImplementedException();
        }

        public override void RequestEnableBt() {
            var builder = new AlertDialog.Builder(MainActivity.MainContext);
            builder.SetTitle(REQUEST_BT_TITLE).SetMessage(REQUEST_BT_MESSAGE);
            builder.SetPositiveButton(REQUEST_BT_CONFIRM, (src, which) => {
                btAdapter.Enable();
            });
            builder.SetNegativeButton(REQUEST_BT_DENY, (src, which) => { });
            builder.Create().Show();
        }

        public override BtError ScanForDevices() {
            throw new NotImplementedException();
        }

        public override void ScanForService(string service, bool scanFor = true) {
            if (scanFor && !ScanServices.Contains(service.ToUpper())) {
                ScanServices.Add(service.ToUpper());
            } else if (!scanFor && ScanServices.Contains(service.ToUpper())) {
                ScanServices.Remove(service.ToUpper());
            }
        }

        public override void StopScanning() {
            throw new NotImplementedException();
        }

        public override void SubscribeToCharacteristic(string characteristic, bool subscribe = true) {
            throw new NotImplementedException();
        }

        public override void WriteCharacteristic(string characteristic, byte[] data) {
            throw new NotImplementedException();
        }

        public override void WriteDescriptor(string descriptor, byte[] data) {
            throw new NotImplementedException();
        }
    }
}