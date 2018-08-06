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
using System.Diagnostics;


// NOTE: All async methods are private and are forced to run syncrenously in order to work the same way as other platforms do
//       This is required to conform to the BLEClient interface.

namespace ProtoRIOControl.UWP.Bluetooth {
    public class UWPBluetooth : IBluetooth{

        // Platform Specific Objects
        private List<GattDeviceService> serviceObjects = new List<GattDeviceService>();
        private List<GattCharacteristic> characteristicObjects = new List<GattCharacteristic>();
        private List<GattDescriptor> descriptorObjects = new List<GattDescriptor>();
        private List<string> scanServices = new List<string>();

        // Which characteristics we are subscribed to notifications from
        private List<GattCharacteristic> subscribeCharacteristics = new List<GattCharacteristic>();

        // Devices
        List<ulong> deviceAddresses = new List<ulong>();
        List<BluetoothLEDevice> devices = new List<BluetoothLEDevice>();


        // UWP Bluetooth stuff
        BluetoothLEAdvertisementWatcher watcher;
        BluetoothLEDevice connectedDevice;
        BluetoothAdapter adapter;

        // The delegate
        private BTCallback callback;

        private bool isScanning = false;
        private bool _isConnected = false;

        private CoreDispatcher mainThread = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;

#pragma warning disable 4014
        public UWPBluetooth(BTCallback btCallback) {
            this.callback = btCallback;
            Task.Run(async () => {
                BtError error = await _checkBtSupport();
                if (error != BtError.NoBluetooth && error != BtError.NoBLE) {
                    var lastState = error == BtError.None;
                    // Watch for bt power changes
                    while (true) {
                        BtError e = await _checkBtSupport();
                        var state = e == BtError.None;
                        if (state != lastState) {
                            lastState = state;
                            // Should not wait for this. That would slow down checking for power changes
                            callback.onBluetoothPowerChanged(state);
                        }
                        await Task.Delay(100);
                    }
                }
            });
        }
#pragma warning restore 4014

        public void scanForService(string service) {
            if (!scanServices.Contains(service.ToUpper())) {
                scanServices.Add(service.ToUpper());
            }
        }

        private async Task<BtError> _checkBtSupport() {
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
        public BtError checkBtSupport() {
            var task = Task<BtError>.Run(async () => {
                return await _checkBtSupport();
            });
            task.Wait();
            return task.Result;
        }

#pragma warning disable 4014
        public void showEnableBtPrompt(string title, string message, string confirmText, string cancelText) {
            // This should not be awaited. This function needs to return like it does on other platforms
            // This code must run on the main thread because it is UI code
            mainThread.RunAsync(CoreDispatcherPriority.Normal, async () => {
                ContentDialog locationPromptDialog = new ContentDialog {
                    Title = title,
                    Content = message,
                    CloseButtonText = cancelText,
                    PrimaryButtonText = confirmText
                };
                ContentDialogResult result = await locationPromptDialog.ShowAsync();
                if (result == ContentDialogResult.Primary) {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(@"ms-settings:bluetooth"));
                }
            });
        }
#pragma warning restore 4014

        private async Task<BtError> _enumerateDevices() {
            if (!isScanning && !_isConnected) {
                devices.Clear();
                deviceAddresses.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                subscribeCharacteristics.Clear();
                BtError error = await _checkBtSupport();
                if (error != BtError.None) {
                    return error;
                }
                watcher = new BluetoothLEAdvertisementWatcher();
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                watcher.Received += DeviceDiscovered;
                watcher.Start();
                isScanning = true;
                return BtError.None;
            } else {
                return BtError.AlreadyRunning;
            }
        }
        public BtError enumerateDevices() {
            var task = Task<BtError>.Run(async () => {
                return await _enumerateDevices();
            });
            task.Wait();
            return task.Result;
        }

        public void endEnumeration() {
            if (isScanning) {
                watcher.Stop();
                watcher = null;
                isScanning = false;
            }
        }

        private async Task _connect(string deviceAddress) {
            BluetoothLEDevice device = devices.First(f => (f.BluetoothAddress + "").ToUpper().Equals(deviceAddress.ToUpper()));
            GattDeviceServicesResult result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (result.Status == GattCommunicationStatus.Success) {
                _isConnected = true;
                connectedDevice = device;
                device.ConnectionStatusChanged += ConnectionStatusChanged;
                foreach (GattDeviceService service in result.Services) {
                    await AddService(service);
                }
                callback.onConnectToDevice((device.BluetoothAddress + "").ToUpper(), device.Name, true);
            } else {
                callback.onConnectToDevice((device.BluetoothAddress + "").ToUpper(), device.Name, false);
            }
        }
        public void connect(string deviceAddress) {
            var task = Task.Run(async () => {
                await _connect(deviceAddress);
            });
            //task.Wait();
        }

        public void disconnect() {
            if (_isConnected) {
                // Don't need to watch value changes anymore
                foreach (GattCharacteristic c in characteristicObjects) {
                    c.ValueChanged -= CharacteristicValueChanged;
                }
                var task = Task.Run(async () => {
                    // Unsubscribe from characteristics
                    foreach (GattCharacteristic c in subscribeCharacteristics) {
                        await _subscribeToCharacteristic(c.Uuid.ToString().ToUpper(), false);
                    }
                });
                task.Wait();
                int i = devices.IndexOf(connectedDevice);
                connectedDevice.Dispose();
                connectedDevice = null;
                // Make sure the disposed object will not be used again
                // Force it to get a new device id
                devices.RemoveAt(i);
                deviceAddresses.RemoveAt(i);
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                subscribeCharacteristics.Clear();
                _isConnected = false;
            }
        }

        /// <summary>
        /// Subscribe to a characteristic to receive notifications when its value is changed
        /// </summary>
        /// <param name="characteristic">The characteristic to subscribe to</param>
        /// <param name="subscribe">Whether not to subscribe to the characteristic (false to unsubscribe)</param>
        private async Task _subscribeToCharacteristic(string characteristic, bool subscribe = true) {
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

        public void subscribeToUartChars() {
            var task = Task.Run(async () => {
                await _subscribeToCharacteristic(BTValues.txCharacteristic, true);
            });
            //task.Wait();
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
                            }
                        }
                        characteristicObjects.Add(c);
                    }
                }
                serviceObjects.Add(service);
            }
        }

        private async Task _writeToUart(byte[] value) {
            GattCharacteristic c = GetCharacteristic(new Guid(BTValues.rxCharacteristic));
            if (c == null) {
                return;
            }
            GattWriteResult result = null;
            try {
                result = await c.WriteValueWithResultAsync(WindowsRuntimeBufferExtensions.AsBuffer(value));
            } catch (Exception e) {

            }
            if (result?.Status == GattCommunicationStatus.Success) {
                callback.onUartDataSent(value);
            }
        }
        public void writeToUart(byte[] value) {
            var task = Task.Run(async () => {
                await _writeToUart(value);
            });
            //task.Wait();
        }

        public bool hasUartService() {
            foreach (GattDeviceService s in serviceObjects) {
                if (s.Uuid.ToString().ToUpper().Equals(BTValues.uartService.ToUpper()))
                    return true;
            }
            return false;
        }

        public bool isConnected(){
            return _isConnected;
        }

        public bool isEnumerating(){
            return isScanning;
        }

        public void cancelConnect() {
            _isConnected = true; // Force the disconnect method to run
            try { disconnect(); } catch (Exception e) { }
        }

        #region Event Handlers
        // AdvertisementWatcher Detected device
        private async void DeviceDiscovered(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args) {
            bool returnDevice = scanServices.Count == 0;
            if (!returnDevice) {
                foreach (Guid uuid in args.Advertisement.ServiceUuids) {
                    returnDevice = scanServices.Contains(uuid.ToString().ToUpper());
                    if (returnDevice)
                        break;
                }
            }
            if (returnDevice) {
                BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                if(device != null) {
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
                    callback.onDeviceDiscovered((device.BluetoothAddress + "").ToUpper(), advertisedName, args.RawSignalStrengthInDBm);
                } else {
                    Debug.WriteLine("-------------Error:------------");
                    Debug.WriteLine("Devie with address " + args.BluetoothAddress + " was a null device!");
                    Debug.WriteLine("-------------------------------");
                }
            }
        }
        private void CharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args) {
            if (sender.Uuid.ToString().ToUpper().Equals(BTValues.txCharacteristic.ToUpper())) {
                callback.onUartDataReceived(args.CharacteristicValue.ToArray());
            }
        }
        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args) {
            callback.onDisconnectFromDevice((sender.BluetoothAddress + "").ToUpper(), sender.Name);
        }
        #endregion
    }
}
