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
    public class AndroidBLEClientBuilder : BLEClientBuilder {
        public BLEClient Create(BLEDelegate bleDelegate) {
            return new AndroidBLEClient(bleDelegate);
        }
    }

    public class AndroidBLEClient : BLEClient {

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
            scanCallback = new MyScanCallback(this);
            bluetoothGattCallback = new MyGattCallback(this);
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
            if (gattConnection != null) {
                Disconnect();
            }
            var device = devices.FirstOrDefault((dev) => dev.Address.ToUpper().Equals(deviceAddress.ToUpper()));
            if (device != null) {
                gattConnection = device.ConnectGatt(MainActivity.MainContext, false, bluetoothGattCallback);
                gattConnection.Connect();
            }
        }

        public override void Disconnect() {
            if (IsConnected) {
                Services.Clear();
                Characteristics.Clear();
                Descriptors.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                gattConnection?.Disconnect();
                gattConnection = null;
                IsConnected = false;
            }
        }

        public override bool HasCharacteristic(string characteristic) {
            return Characteristics.Contains(characteristic.ToUpper());
        }

        public override bool HasDescriptor(string descriptor) {
            return Descriptors.Contains(descriptor.ToUpper());
        }

        public override bool HasService(string service) {
            return Services.Contains(service.ToUpper());
        }

        public override void ReadCharacteristic(string characteristic) {
            var c = getCharacteristic(UUID.FromString(characteristic));
            if (c != null) {
                gattConnection.ReadCharacteristic(c);
            } else {
                mainThread.Post(() => {
                    Delegate.OnCharacteristicRead(characteristic.ToUpper(), false, null);
                });
            }
        }

        public override void ReadDescriptor(string descriptor) {
            var desc = getDescriptor(UUID.FromString(descriptor));
            if (desc != null) {
                gattConnection.ReadDescriptor(desc);
            } else {
                mainThread.Post(() => {
                    Delegate.OnDescriptorRead(descriptor.ToUpper(), false, null);
                });
            }
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
            if (!IsScanning && !IsConnected) {
                devices.Clear();
                deviceAddresses.Clear();
                Services.Clear();
                Characteristics.Clear();
                Descriptors.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                var error = CheckBluetooth();
                if (error != BtError.None) {
                        return error;
                }
                var settings = new ScanSettings.Builder().SetScanMode(Android.Bluetooth.LE.ScanMode.Balanced).Build();
                var filters = new List<ScanFilter>();
                ScanServices.ForEach((item) => {
                    filters.Add(new ScanFilter.Builder().SetServiceUuid(new ParcelUuid(UUID.FromString(item))).Build());
                });
                btAdapter.BluetoothLeScanner.StartScan(filters, settings, scanCallback);
                IsScanning = true;
                return BtError.None;
            } else {
                return BtError.AlreadyRunning;
            }
        }

        public override void ScanForService(string service, bool scanFor = true) {
            if (scanFor && !ScanServices.Contains(service.ToUpper())) {
                ScanServices.Add(service.ToUpper());
            } else if (!scanFor && ScanServices.Contains(service.ToUpper())) {
                ScanServices.Remove(service.ToUpper());
            }
        }

        public override void StopScanning() {
            if (IsScanning) {
                btAdapter.BluetoothLeScanner.StopScan(scanCallback);
                IsScanning = false;
            }
        }

        public override void SubscribeToCharacteristic(string characteristic, bool subscribe = true) {
            var c = getCharacteristic(UUID.FromString(characteristic));
            if (c != null) {
                gattConnection.SetCharacteristicNotification(c, subscribe);
                var descriptor = c.GetDescriptor(UUID.FromString("00002902-0000-1000-8000-00805f9b34fb")); // Client characteristic config UUID
                if (descriptor != null) {
                    descriptor.SetValue((subscribe ? BluetoothGattDescriptor.EnableNotificationValue : BluetoothGattDescriptor.DisableNotificationValue).ToArray());
                    gattConnection.WriteDescriptor(descriptor);
                }
            }
        }

        public override void WriteCharacteristic(string characteristic, byte[] data) {
            var c = getCharacteristic(UUID.FromString(characteristic));
            if (c != null) {
                c.SetValue(data);
                if (gattConnection.WriteCharacteristic(c) != true) {
                    mainThread.Post(() => {
                        // If there is no write permission the onCharacteristicWrite BluetoothGattCallback method is never called
                        Delegate.OnCharacteristicWrite(characteristic.ToUpper(), false, null);
                    });
                }
            } else {
                mainThread.Post(() => {
                    Delegate.OnCharacteristicWrite(characteristic.ToUpper(), false, null);
                });
            }
        }

        public override void WriteDescriptor(string descriptor, byte[] data) {
            var desc = getDescriptor(UUID.FromString(descriptor));
            if (desc != null) {
                desc.SetValue(data);
                gattConnection.WriteDescriptor(desc);
            } else {
                mainThread.Post(() => {
                    Delegate.OnDescriptorWrite(descriptor.ToUpper(), false, null);
                });
            }
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
                                Descriptors.Add(d.Uuid.ToString().ToUpper());
                            }
                        }
                        characteristicObjects.Add(c);
                        Characteristics.Add(c.Uuid.ToString().ToUpper());
                    }
                }
                serviceObjects.Add(service);
                Services.Add(service.Uuid.ToString().ToUpper());
            }
        }

        MyScanCallback scanCallback;
        class MyScanCallback : ScanCallback {

            AndroidBLEClient client;
            public MyScanCallback(AndroidBLEClient client) {
                this.client = client;
            }

            public override void OnScanFailed(ScanFailure errorCode) {
                base.OnScanFailed(errorCode);
            }
            public override void OnScanResult(ScanCallbackType callbackType, ScanResult result) {
                base.OnScanResult(callbackType, result);
                if (result != null) {
                    if (!client.deviceAddresses.Contains(result.Device.Address)) {
                        client.devices.Add(result.Device);
                        client.deviceAddresses.Add(result.Device.Address);
                    }
                    client.mainThread.Post(() => {
                        client.Delegate.OnDeviceDiscovered(result.Device.Address.ToUpper(), result.Device.Name, result.Rssi);
                    });
                }
            }
            public override void OnBatchScanResults(IList<ScanResult> results) {
                base.OnBatchScanResults(results);
                if (results != null) {
                    foreach(var result in results){
                        client.devices.Add(result.Device);
                        client.mainThread.Post(() => {
                            client.Delegate.OnDeviceDiscovered(result.Device.Address.ToUpper(), result.Device.Name, result.Rssi);
                        });
                    }
                }
            }
        }

        MyGattCallback bluetoothGattCallback;
        class MyGattCallback : BluetoothGattCallback {
            private AndroidBLEClient client;
            public MyGattCallback(AndroidBLEClient client) {
                this.client = client;
            }

            public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status) {
                base.OnCharacteristicRead(gatt, characteristic, status);
                if (characteristic != null) {
                    client.mainThread.Post(() => {
                        client.Delegate.OnCharacteristicRead(characteristic.Uuid.ToString().ToUpper(), status == GattStatus.Success, characteristic.GetValue());
                    });
                }
            }
            public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status) {
                base.OnCharacteristicWrite(gatt, characteristic, status);
                if (characteristic != null) {
                    client.mainThread.Post(() => {
                        client.Delegate.OnCharacteristicWrite(characteristic.Uuid.ToString().ToUpper(), status == GattStatus.Success, characteristic.GetValue());
                    });
                }
            }
            public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status) {
                base.OnServicesDiscovered(gatt, status);
                if (gatt != null) {
                    foreach(var service in gatt.Services) {
                        client.AddService(service);
                    }
                    client.mainThread.Post(() => {
                        client.Delegate.OnServicesDiscovered();
                    });
                }
            }
            public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status) {
                base.OnDescriptorWrite(gatt, descriptor, status);
                if (descriptor != null) {
                    client.mainThread.Post(() => {
                        client.Delegate.OnDescriptorWrite(descriptor.Uuid.ToString().ToUpper(), status == GattStatus.Success, descriptor.GetValue());
                    });
                }
            }
            public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic) {
                base.OnCharacteristicChanged(gatt, characteristic);
                if (characteristic != null) {
                    client.mainThread.Post(() => {
                        client.Delegate.OnCharacteristicRead(characteristic.Uuid.ToString().ToUpper(), true, characteristic.GetValue());
                    });
                }
            }
            public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status) {
                base.OnDescriptorRead(gatt, descriptor, status);
                if (descriptor != null) {
                    client.mainThread.Post(() => {
                        client.Delegate.OnDescriptorRead(descriptor.Uuid.ToString().ToUpper(), status == GattStatus.Success, descriptor.GetValue());
                    });
                }
            }
            public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState) {
                base.OnConnectionStateChange(gatt, status, newState);
                if (gatt != null) {
                    System.Diagnostics.Debug.Write("Status " + status);
                    if (newState == ProfileState.Connected && status == GattStatus.Success) {
                        gatt.DiscoverServices();
                        client.mainThread.Post(() => {
                            client.Delegate.OnConnectToDevice(gatt.Device.Address.ToUpper(), gatt.Device.Name, true);
                        });
                        client.IsConnected = true;
                    }
                    if (newState == ProfileState.Disconnected) {
                        client.Services.Clear();
                        client.Characteristics.Clear();
                        client.Descriptors.Clear();
                        client.mainThread.Post(() => {
                            client.Delegate.OnDisconnectFromDevice(gatt.Device.Address.ToUpper(), gatt.Device.Name);
                        });
                        client.IsConnected = false;
                    }
                }
            }
        }
    }
}
