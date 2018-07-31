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
using Java.Util;
using ProtoRIO.Bluetooth;

namespace ProtoRIOControl.Droid.Bluetooth {
    public class AndroidBluetooth : IBluetooth {

        // Platform Specific Objects
        private List<BluetoothGattService> serviceObjects = new List<BluetoothGattService>();
        private List<BluetoothGattCharacteristic> characteristicObjects = new List<BluetoothGattCharacteristic>();
        private List<BluetoothGattDescriptor> descriptorObjects = new List<BluetoothGattDescriptor>();
        private List<string> scanServices = new List<string>();

        // Keep track of detected devices
        private List<string> deviceAddresses = new List<string>();
        private List<BluetoothDevice> devices = new List<BluetoothDevice>();

        // Android Specific Bluetooth Objects
        private BluetoothManager btManager;
        private BluetoothAdapter btAdapter;
        private BluetoothGatt gattConnection = null;

        private bool isScanning = false;
        private bool isConnected = false;

        Thread btCheckThread;

        BTCallback callback;

        public AndroidBluetooth(BTCallback btCallback) {
            this.callback = btCallback;
            btManager = (BluetoothManager)MainActivity.MainContext.GetSystemService(Context.BluetoothService);
            btAdapter = btManager.Adapter;
            scanCallback = new MyScanCallback(this);
            bluetoothGattCallback = new MyGattCallback(this);
            // Watch for Bluetooth Power Changes
            btCheckThread = new Thread(new ThreadStart(() => {
                bool lastState = false;
                while (true && btAdapter != null) {
                    bool state = btAdapter.IsEnabled;
                    if (state != lastState) {
                        lastState = state;
                        if (!state) {
                            endEnumeration();
                            disconnect();
                        }
                        callback.onBluetoothPowerChanged(state);
                    }
                    Thread.Sleep(100);
                }
            }));
            btCheckThread.Start();
        }

        public BtError checkBtSupport() {
            if (!MainActivity.MainContext.PackageManager.HasSystemFeature(PackageManager.FeatureBluetooth))
                return BtError.NoBluetooth;
            if (!MainActivity.MainContext.PackageManager.HasSystemFeature(PackageManager.FeatureBluetoothLe))
                return BtError.NoBLE;
            if (btAdapter == null || !btAdapter.IsEnabled)
                return BtError.Disabled;
            return BtError.None;
        }

        public void connect(string deviceAddress) {
            if (gattConnection != null) {
                disconnect();
            }
            var device = devices.FirstOrDefault((dev) => dev.Address.ToUpper().Equals(deviceAddress.ToUpper()));
            if (device != null) {
                gattConnection = device.ConnectGatt(MainActivity.MainContext, false, bluetoothGattCallback);
                gattConnection.Connect();
            }
        }

        public void disconnect() {
            if (isConnected) {
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                gattConnection?.Disconnect();
                gattConnection = null;
                isConnected = false;
            }
        }

        public void showEnableBtPrompt(string title, string message, string confirmText, string cancelText) {
            var builder = new AlertDialog.Builder(MainActivity.MainContext);
            builder.SetTitle(title).SetMessage(message);
            builder.SetPositiveButton(confirmText, (src, which) => {
                Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                MainActivity.MainContext.StartActivity(enableBtIntent);
            });
            builder.SetNegativeButton(cancelText, (src, which) => { });
            builder.Create().Show();
        }

        public BtError enumerateDevices() {
            if (!isScanning && !isConnected) {
                devices.Clear();
                deviceAddresses.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                var error = checkBtSupport();
                if (error != BtError.None) {
                        return error;
                }
                var settings = new ScanSettings.Builder().SetScanMode(Android.Bluetooth.LE.ScanMode.Balanced).Build();
                var filters = new List<ScanFilter>();
                scanServices.ForEach((item) => {
                    filters.Add(new ScanFilter.Builder().SetServiceUuid(new ParcelUuid(UUID.FromString(item))).Build());
                });
                if (filters.Count == 0)
                    btAdapter.BluetoothLeScanner.StartScan(scanCallback);
                else
                    btAdapter.BluetoothLeScanner.StartScan(filters, settings, scanCallback);
                isScanning = true;
                return BtError.None;
            } else {
                return BtError.AlreadyRunning;
            }
        }

        public void scanForService(string service) {
            if (!scanServices.Contains(service.ToUpper())) {
                scanServices.Add(service.ToUpper());
            } 
        }

        public void endEnumeration() {
            if (isScanning) {
                if(btAdapter != null)
                    btAdapter.BluetoothLeScanner.StopScan(scanCallback);
                isScanning = false;
            }
        }

        public void subscribeToUartChars() {
            var tx = getCharacteristic(UUID.FromString(BTValues.txCharacteristic));
            if (tx != null) {
                gattConnection.SetCharacteristicNotification(tx, true);
                var descriptor = tx.GetDescriptor(UUID.FromString("00002902-0000-1000-8000-00805f9b34fb")); // Client characteristic config UUID
                if (descriptor != null) {
                    descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                    gattConnection.WriteDescriptor(descriptor);
                }
            }
        }

        public void writeToUart(byte[] data) {
            var rx = getCharacteristic(UUID.FromString(BTValues.rxCharacteristic));
            if (rx != null) {
                rx.SetValue(data);
                gattConnection.WriteCharacteristic(rx);
            }
        }

        public bool hasUartService() {
            foreach(BluetoothGattService s in serviceObjects) {
                if (s.Uuid.ToString().ToUpper().Equals(BTValues.uartService.ToUpper()))
                    return true;
            }
            return false;
        }

        /*
         * Get a desc/char/service from a UUID
         */
        private BluetoothGattDescriptor getDescriptor(UUID uuid) {
            return descriptorObjects.FirstOrDefault((item) => item.Uuid.Equals(uuid));
        }
        private BluetoothGattCharacteristic getCharacteristic(UUID uuid) {
            return characteristicObjects.FirstOrDefault((item) => item.Uuid.Equals(uuid));
        }
        private BluetoothGattService getService(UUID uuid) {
            return serviceObjects.FirstOrDefault((item) => item.Uuid.Equals(uuid));
        }

        // Supoort func to hadle all the mess of enumerating chars and descs
        private void AddService(BluetoothGattService service) {
            if (!serviceObjects.Contains(service)) {
                if (service.Type == GattServiceType.Primary) {
                    foreach(var s in service.IncludedServices) {
                        if (!serviceObjects.Contains(s)) {
                            AddService(s);
                        }
                    }
                }
                foreach(var c in service.Characteristics) {
                    if (!characteristicObjects.Contains(c)) {
                        foreach(var d in c.Descriptors) {
                            if (!descriptorObjects.Contains(d)) {
                                descriptorObjects.Add(d);
                            }
                        }
                        characteristicObjects.Add(c);
                    }
                }
                serviceObjects.Add(service);
            }
        }

        MyScanCallback scanCallback;
        class MyScanCallback : ScanCallback {

            AndroidBluetooth client;
            public MyScanCallback(AndroidBluetooth client) {
                this.client = client;
            }

            public override void OnScanFailed(ScanFailure errorCode) {
                base.OnScanFailed(errorCode);
                System.Diagnostics.Debug.WriteLine("Scan Failed with error: " + errorCode);
            }
            public override void OnScanResult(ScanCallbackType callbackType, ScanResult result) {
                base.OnScanResult(callbackType, result);
                if (result != null) {
                    if (!client.deviceAddresses.Contains(result.Device.Address)) {
                        client.devices.Add(result.Device);
                        client.deviceAddresses.Add(result.Device.Address);
                    }
                    client.callback.onDeviceDiscovered(result.Device.Address.ToUpper(), result.Device.Name, result.Rssi);
                }
            }
            public override void OnBatchScanResults(IList<ScanResult> results) {
                base.OnBatchScanResults(results);
                if (results != null) {
                    foreach(var result in results){
                        client.devices.Add(result.Device);
                        client.callback.onDeviceDiscovered(result.Device.Address.ToUpper(), result.Device.Name, result.Rssi);
                    }
                }
            }
        }

        MyGattCallback bluetoothGattCallback;
        class MyGattCallback : BluetoothGattCallback {
            private AndroidBluetooth client;
            public MyGattCallback(AndroidBluetooth client) {
                this.client = client;
            }

            public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status) {
                base.OnCharacteristicRead(gatt, characteristic, status);
                if (characteristic != null && characteristic.Uuid.ToString().ToUpper().Equals(BTValues.txCharacteristic.ToUpper()) && status == GattStatus.Success) {
                    client.callback.onUartDataReceived(characteristic.GetValue());
                }
            }
            public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status) {
                base.OnCharacteristicWrite(gatt, characteristic, status);
                if (characteristic != null && characteristic.Uuid.ToString().ToUpper().Equals(BTValues.rxCharacteristic.ToUpper())) {
                    client.callback.onUartDataSent(characteristic.GetValue());
                }
            }
            public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status) {
                base.OnServicesDiscovered(gatt, status);
                if (gatt != null) {
                    foreach(var service in gatt.Services) {
                        client.AddService(service);
                    }
                    client.callback.onConnectToDevice(client.gattConnection.Device.Address, client.gattConnection.Device.Name, true);
                }
            }
            public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic) {
                OnCharacteristicRead(gatt, characteristic, GattStatus.Success);
            }
            public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState) {
                base.OnConnectionStateChange(gatt, status, newState);
                if (gatt != null) {
                    if (newState == ProfileState.Connected && status != GattStatus.Success) {
                        gatt.DiscoverServices();
                        client.callback.onConnectToDevice(gatt.Device.Address.ToUpper(), gatt.Device.Name, false);
                        client.isConnected = true;
                    }else if(newState == ProfileState.Connected){
                        gatt.DiscoverServices();
                    }
                    if (newState == ProfileState.Disconnected) {
                        client.serviceObjects.Clear();
                        client.characteristicObjects.Clear();
                        client.descriptorObjects.Clear();
                        client.callback.onDisconnectFromDevice(gatt.Device.Address.ToUpper(), gatt.Device.Name);
                        client.isConnected = false;
                    }
                }
            }
        }
    }
}
