using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.Radios;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Advertisement;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using ProtoRIO.Bluetooth;


// NOTE: All async methods are private and are forced to run syncrenously in order to work the same way as other platforms do
//       This is required to conform to the BLEClient interface.

namespace ProtoRIOControl.UWP.Bluetooth {
    public class UWPBLEClientBuilder : BLEClientBuilder {
        public BLEClient Create(BLEDelegate bleDelegate) {
            return new UWPBLEClient(bleDelegate);
        }
    }
    public class UWPBLEClient : BLEClient{
        #region Variables and Properties

        // Platform Specific Objects
        private List<GattDeviceService> serviceObjects = new List<GattDeviceService>();
        private List<GattCharacteristic> characteristicObjects = new List<GattCharacteristic>();
        private List<GattDescriptor> descriptorObjects = new List<GattDescriptor>();

        // Which characteristics we are subscribed to notifications from
        private List<GattCharacteristic> subscribeCharacteristics = new List<GattCharacteristic>();

        // Devices
        List<ulong> deviceAddresses = new List<ulong>();
        List<BluetoothLEDevice> devices = new List<BluetoothLEDevice>();

        // Enable BT Dialog Text
        public string REQUEST_BT_TITLE = "Bluetooth Required";
        public string REQUEST_BT_MESSAGE = "Please enable bluetooth in Settings";
        public string REQUEST_BT_CONFIRM = "Settings";
        public string REQUEST_BT_DENY = "Cancel";

        // UWP Bluetooth stuff
        BluetoothLEAdvertisementWatcher watcher;
        BluetoothLEDevice connectedDevice;
        BluetoothAdapter adapter;

        // The delegate
        private BLEDelegate clientDelegate;

        private CoreDispatcher mainThread = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;

        #endregion

        public UWPBLEClient(BLEDelegate clientDelegate) {
            this.clientDelegate = clientDelegate;
            Task.Run(async () => {
                BtError error = await _CheckBluetooth();
                if (error != BtError.NoBluetooth && error != BtError.NoBLE && error != BtError.NoServer) {
                    var lastState = error == BtError.None;
                    // Watch for bt power changes
                    while (true) {
                        BtError e = await _CheckBluetooth();
                        var state = e == BtError.None;
                        if (state != lastState) {
                            lastState = state;
                            // Should not wait for this. That would slow down checking for power changes
                            mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                                clientDelegate.OnBluetoothPowerChanged(state);
                            });
                        }
                        await Task.Delay(100);
                    }
                }
            });
        }

        #region Client Control
        public override void ScanForService(string service, bool scanFor = true) {
            if (scanFor && !ScanServices.Contains(service.ToUpper())) {
                ScanServices.Add(service.ToUpper());
            }
            if (!scanFor && ScanServices.Contains(service.ToUpper())) {
                ScanServices.Remove(service);
            }
        }

        private async Task<BtError> _CheckBluetooth() {
            var radios = await Radio.GetRadiosAsync();
            var r = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
            if (r == null) {
                return BtError.NoBluetooth;
            }
            adapter = await BluetoothAdapter.GetDefaultAsync();
            if (adapter != null) {
                if (!adapter.IsLowEnergySupported || !adapter.IsCentralRoleSupported) {
                    return BtError.NoBLE;
                }
            }
            if (r.State != RadioState.On) {
                return BtError.Disabled;
            }
            return BtError.None;
        }
        public override BtError CheckBluetooth() {
            var task = Task<BtError>.Run(async () => {
                return await _CheckBluetooth();
            });
            task.Wait();
            return task.Result;
        }

        private async Task _RequestEnabeBt() {
            ContentDialog locationPromptDialog = new ContentDialog {
                Title = REQUEST_BT_TITLE,
                Content = REQUEST_BT_MESSAGE,
                CloseButtonText = REQUEST_BT_DENY,
                PrimaryButtonText = REQUEST_BT_CONFIRM
            };
            ContentDialogResult result = await locationPromptDialog.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(@"ms-settings:bluetooth"));
            }
        }
        public override void RequestEnableBt() {
            var task = Task.Run(async () => {
                await _RequestEnabeBt();
            });
            task.Wait();
        }

        private async Task<BtError> _ScanForDevices() {
            if (!IsScanning && !IsConnected) {
                devices.Clear();
                deviceAddresses.Clear();
                Services.Clear();
                Characteristics.Clear();
                Descriptors.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                subscribeCharacteristics.Clear();
                BtError error = await _CheckBluetooth();
                if (error != BtError.None) {
                    return error;
                }
                watcher = new BluetoothLEAdvertisementWatcher();
                watcher.Received += DeviceDiscovered;
                watcher.Start();
                IsScanning = true;
                return BtError.None;
            } else {
                return BtError.AlreadyRunning;
            }
        }
        public override BtError ScanForDevices() {
            var task = Task<BtError>.Run(async () => {
                return await _ScanForDevices();
            });
            task.Wait();
            return task.Result;
        }

        public override void StopScanning() {
            if (IsScanning) {
                watcher.Stop();
                watcher = null;
                IsScanning = false;
            }
        }

        private async Task _ConnectToDevice(string deviceAddress) {
            BluetoothLEDevice device = devices.First(f => (f.BluetoothAddress + "").ToUpper().Equals(deviceAddress.ToUpper()));
            GattDeviceServicesResult result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (result.Status == GattCommunicationStatus.Success) {
                IsConnected = true;
                connectedDevice = device;
                device.ConnectionStatusChanged += ConnectionStatusChanged;
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnConnectToDevice((device.BluetoothAddress + "").ToUpper(), device.Name, true);
                });
                foreach (GattDeviceService service in result.Services) {
                    await AddService(service);
                }
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnServicesDiscovered();
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnConnectToDevice((device.BluetoothAddress + "").ToUpper(), device.Name, false);
                });
            }
        }

        public override void ConnectToDevice(string deviceAddress) {
            var task = Task.Run(async () => {
                await _ConnectToDevice(deviceAddress);
            });
            task.Wait();
        }

        public override void Disconnect() {
            if (IsConnected) {
                // Don't need to watch value changes anymore
                foreach (GattCharacteristic c in characteristicObjects) {
                    c.ValueChanged -= CharacteristicValueChanged;
                }
                // Unsubscribe from characteristics
                foreach (GattCharacteristic c in subscribeCharacteristics) {
                    SubscribeToCharacteristic(c.Uuid.ToString(), false);
                }
                int i = devices.IndexOf(connectedDevice);
                connectedDevice.Dispose();
                connectedDevice = null;
                // Make sure the disposed object will not be used again
                // Force it to get a new device id
                devices.RemoveAt(i);
                deviceAddresses.RemoveAt(i);
                Services.Clear();
                Characteristics.Clear();
                Descriptors.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                subscribeCharacteristics.Clear();
                IsConnected = false;
            }
        }

        private async Task _SubscribeToCharacteristic(string characteristic, bool subscribe) {
            GattCharacteristic c = GetCharacteristic(new Guid(characteristic));
            if (c == null)
                return;
            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.None;
            if (subscribe) {
                if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)) {
                    cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                } else if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)) {
                    cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                }
            }
            GattCommunicationStatus result = GattCommunicationStatus.ProtocolError;
            try {
                result = await c.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);
            } catch (Exception e) {

            }
            if (result != GattCommunicationStatus.Success) {
                //Check Bt Permissions
            } else {
                subscribeCharacteristics.Add(c);
                c.ValueChanged += CharacteristicValueChanged;
            }
        }
        public override void SubscribeToCharacteristic(string characteristic, bool subscribe = true) {
            var task = Task.Run(async () => {
                await _SubscribeToCharacteristic(characteristic, subscribe);
            });
            task.Wait();
        }

        private GattDeviceService GetService(Guid service) {
            return serviceObjects.FirstOrDefault(s => s.Uuid.Equals(service));
        }

        private GattCharacteristic GetCharacteristic(Guid characteristic) {
            return characteristicObjects.FirstOrDefault(c => c.Uuid.Equals(characteristic));
        }

        private GattDescriptor GetDescriptor(Guid descriptor) {
            return descriptorObjects.FirstOrDefault(d => d.Uuid.Equals(descriptor));
        }
        
        /// <summary>
        ///  Add a service (and it's included services, characteristics, and descriptors) to the lists
        /// </summary>
        /// <param name="service">The service to add</param>
        private async Task AddService(GattDeviceService service) {
            if (!serviceObjects.Contains(service)) {
                GattDeviceServicesResult res = await service.GetIncludedServicesAsync();
                if (res.Status == GattCommunicationStatus.Success) {
                    foreach (GattDeviceService s in res.Services) {
                        await AddService(s);
                    }
                }
                GattCharacteristicsResult charRes = await service.GetCharacteristicsAsync();
                if (charRes.Status == GattCommunicationStatus.Success) {
                    foreach (GattCharacteristic c in charRes.Characteristics) {
                        GattDescriptorsResult descRes = await c.GetDescriptorsAsync();
                        if (descRes.Status == GattCommunicationStatus.Success) {
                            foreach (GattDescriptor d in descRes.Descriptors) {
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

        #endregion

        #region Characteristics and Descriptors
        private async Task _ReadCharacteristic(string characteristic) {
            GattCharacteristic c = GetCharacteristic(new Guid(characteristic));
            if (c == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicRead(characteristic.ToUpper(), false, null);
                });
                return;
            }
            GattReadResult result = null;
            try {
                result = await c.ReadValueAsync();
            } catch (Exception e) {

            }
            if (result != null && result.Status == GattCommunicationStatus.Success) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicRead(characteristic.ToUpper(), true, result.Value.ToArray());
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicRead(characteristic.ToUpper(), false, null);
                });
            }
        }
        public override void ReadCharacteristic(string characteristic) {
            var task = Task.Run(async () => {
                await _ReadCharacteristic(characteristic);
            });
            task.Wait();
        }

        private async Task _WriteCharacteristic(string characteristic, byte[] value) {
            GattCharacteristic c = GetCharacteristic(new Guid(characteristic));
            if (c == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicWrite(characteristic.ToUpper(), false, null);
                });
                return;
            }
            GattWriteResult result = null;
            try {
                result = await c.WriteValueWithResultAsync(WindowsRuntimeBufferExtensions.AsBuffer(value));
            } catch (Exception e) {

            }
            if (result?.Status == GattCommunicationStatus.Success) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicWrite(characteristic.ToUpper(), true, value);
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicWrite(characteristic.ToUpper(), false, null);
                });
            }
        }
        public override void WriteCharacteristic(string characteristic, byte[] value) {
            var task = Task.Run(async () => {
                await _WriteCharacteristic(characteristic, value);
            });
            task.Wait();
        }

        private async Task _ReadDescriptor(string descriptor) {
            GattDescriptor d = GetDescriptor(new Guid(descriptor));
            if (d == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorRead(descriptor.ToUpper(), false, null);
                });
                return;
            }
            GattReadResult result = null;
            try {
                result = await d.ReadValueAsync();
            } catch (Exception e) {

            }
            if (result?.Status == GattCommunicationStatus.Success) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorRead(descriptor.ToUpper(), true, result.Value.ToArray());
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorRead(descriptor.ToUpper(), false, null);
                });
            }
        }
        public override void ReadDescriptor(string descriptor) {
            var task = Task.Run(async () => {
                await _ReadDescriptor(descriptor);
            });
            task.Wait();
        }

        private async Task _WriteDescriptor(string descriptor, byte[] value) {
            GattDescriptor d = GetDescriptor(new Guid(descriptor));
            if (d == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorWrite(descriptor.ToUpper(), false, null);
                });
                return;
            }
            GattWriteResult result = null;
            try {
                result = await d.WriteValueWithResultAsync(WindowsRuntimeBufferExtensions.AsBuffer(value));
            } catch (Exception e) {

            }
            if (result?.Status == GattCommunicationStatus.Success) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorWrite(descriptor.ToUpper(), true, value);
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorWrite(descriptor.ToUpper(), false, null);
                });
            }
        }
        public override void WriteDescriptor(string descriptor, byte[] value) {
            var task = Task.Run(async () => {
                await _WriteDescriptor(descriptor, value);
            });
            task.Wait();
        }

        public override bool HasService(string service) {
            return Services.Contains(service.ToUpper());
        }

        public override bool HasCharacteristic(string characteristic) {
            return Characteristics.Contains(characteristic.ToUpper());
        }

        public override bool HasDescriptor(string descriptor) {
            return Descriptors.Contains(descriptor.ToUpper());
        }
        #endregion

        #region Event Handlers
        // AdvertisementWatcher Detected device
        private async void DeviceDiscovered(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args) {
            bool returnDevice = ScanServices.Count == 0;
            if (!returnDevice) {
                foreach (Guid uuid in args.Advertisement.ServiceUuids) {
                    returnDevice = ScanServices.Contains(uuid.ToString().ToUpper());
                    if (returnDevice)
                        break;
                }
            }
            if (returnDevice) {
                BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                if (!deviceAddresses.Contains(device.BluetoothAddress)) {
                    devices.Add(device);
                    deviceAddresses.Add(device.BluetoothAddress);
                }
                string advertisedName = device.Name;
                List<BluetoothLEAdvertisementDataSection> complete = new List<BluetoothLEAdvertisementDataSection>(args.Advertisement.GetSectionsByType(BluetoothLEAdvertisementDataTypes.CompleteLocalName));
                List<BluetoothLEAdvertisementDataSection> smallName = new List<BluetoothLEAdvertisementDataSection>(args.Advertisement.GetSectionsByType(BluetoothLEAdvertisementDataTypes.ShortenedLocalName));
                if (complete.Count > 0) {
                    advertisedName = Encoding.UTF8.GetString(complete[0].Data.ToArray());
                } else if (smallName.Count > 0) {
                    advertisedName = Encoding.UTF8.GetString(smallName[0].Data.ToArray());
                }
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDeviceDiscovered((device.BluetoothAddress + "").ToUpper(), advertisedName, args.RawSignalStrengthInDBm);
                });
            }
        }
        private async void CharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args) {
            await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                clientDelegate.OnCharacteristicRead(sender.Uuid.ToString().ToUpper(), true, args.CharacteristicValue.ToArray());
            });
        }
        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args) {
            clientDelegate.OnDisconnectFromDevice((sender.BluetoothAddress + "").ToUpper(), sender.Name);
        }
        #endregion
    }
}
