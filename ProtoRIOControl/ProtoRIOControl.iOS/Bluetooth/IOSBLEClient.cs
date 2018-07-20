using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Foundation;
using ProtoRIO.Bluetooth;
using UIKit;
using CoreBluetooth;

namespace ProtoRIOControl.iOS.Bluetooth {
    public class IOSBLEClientBuilder : BLEClientBuilder {
        public BLEClient Create(BLEDelegate bleDelegate) {
            return new IOSBLEClient(bleDelegate);
        }
    }
    class BLEDescriptor : CBMutableDescriptor {
        public NSObject dynamicValue;
        public BLEDescriptor(CBUUID UUID, NSObject value): base(UUID, null) {
            dynamicValue = value;
        }
        public BLEDescriptor(CBDescriptor descriptor) : base(descriptor.UUID, null) {
            dynamicValue = descriptor.Value;
        }
    }
    class BLECharacteristic : CBMutableCharacteristic {
        public NSData dynamicValue;
        public BLECharacteristic(CBUUID UUID, CBCharacteristicProperties properties, NSData value, CBAttributePermissions permissions): base(UUID, properties, null, permissions) {
            dynamicValue = value;
        }
        public BLECharacteristic(CBCharacteristic characteristic): base(characteristic.UUID, characteristic.Properties, null, CBAttributePermissions.Readable) {
            dynamicValue = characteristic.Value;
        }
    }

    public class IOSBLEClient : BLEClient {

        // Platform Specific Objects
        private List<CBService> serviceObjects = new List<CBService>();
        private List<BLECharacteristic> characteristicObjects = new List<BLECharacteristic>();
        private List<BLEDescriptor> descriptorObjects = new List<BLEDescriptor>();

        // Enable BT Dialog Text
        string REQUEST_BT_TITLE = "Bluetooth Required";
        string REQUEST_BT_MESSAGE = "Please enable bluetooth in Settings.";
        string REQUEST_BT_CONFIRM = "Settings";
        string REQUEST_BT_DENY = "Cancel";

        //Apple Specific Bluetooth Objects
        private CBCentralManager centralManager;
        private CBPeripheral connectedPeripheral;

        // keep track of discovered devices
        private List<string> deviceAddresses = new List<string>();
        private List<CBPeripheral> devices = new List<CBPeripheral>();

        public IOSBLEClient(BLEDelegate bleDelegate) {
            cmDelegate = new MyCentralmanagerDelegate(this);
            peripheralDelegate = new MyCBPeripheralDelegate(this);
            Delegate = bleDelegate;
            CBCentralInitOptions opts = new CBCentralInitOptions();
            opts.ShowPowerAlert = false;
            centralManager = new CBCentralManager(cmDelegate, null, opts);
        }

        public override void ScanForService(string service, bool scanFor = true) {
            if (scanFor && !ScanServices.Contains(service.ToUpper())) {
                ScanServices.Add(service.ToUpper());
            } else if (!scanFor && ScanServices.Contains(service.ToUpper())) {
                ScanServices.RemoveAt(ScanServices.IndexOf(service.ToUpper()));
            }
        }

        public override BtError CheckBluetooth() {
            CBCentralManagerState error = centralManager.State;
            switch(error){
                case CBCentralManagerState.PoweredOn:
                    return BtError.None;
                case CBCentralManagerState.PoweredOff:
                    return BtError.Disabled;
                case CBCentralManagerState.Unsupported:
                    return BtError.NoBluetooth;
                default:
                    return BtError.Unknown;
                    
            }
        }

        public override void RequestEnableBt() {
            var vc = UIApplication.SharedApplication.KeyWindow.RootViewController;
            var alert = UIAlertController.Create(REQUEST_BT_TITLE, REQUEST_BT_MESSAGE, UIAlertControllerStyle.Alert);
            var cancelAction = UIAlertAction.Create(REQUEST_BT_DENY, UIAlertActionStyle.Cancel, (action) => {
                alert.DismissViewController(true, null);
            });
            var settingsAction = UIAlertAction.Create(REQUEST_BT_CONFIRM, UIAlertActionStyle.Default, (Action) => {
                var btSettings = new NSUrl("App-Prefs:root=Bluetooth");
                if (btSettings != null) {
                    if (UIApplication.SharedApplication.CanOpenUrl(btSettings)) {
                        if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0)) {
                            UIApplication.SharedApplication.OpenUrl(btSettings, new UIApplicationOpenUrlOptions(), null);
                        } else {
                            UIApplication.SharedApplication.OpenUrl(btSettings);
                        }
                    }
                }
                alert.DismissViewController(true, null);
            });
            alert.AddAction(settingsAction);
            alert.AddAction(cancelAction);
            vc.PresentViewController(alert, true, null);
        }

        public override BtError ScanForDevices() {
            if (!IsScanning) {
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
                var uuids = new List<CBUUID>();
                foreach(var s in ScanServices){
                    uuids.Add(CBUUID.FromString(s));
                }
                var options = new PeripheralScanningOptions();
                options.AllowDuplicatesKey = true;
                centralManager.ScanForPeripherals(uuids.ToArray(), options);
                IsScanning = true;
                return BtError.None;
            }else{
                return BtError.AlreadyRunning;
            }
        }

        public override void StopScanning() {
            if (IsScanning) {
                centralManager.StopScan();
                IsScanning = false;
            }
        }

        public override void ConnectToDevice(string deviceAddress) {
            if (connectedPeripheral != null) {
                Disconnect();
            }
            var dev = devices.FirstOrDefault((item) => item.Identifier.ToString().ToUpper().Equals(deviceAddress.ToUpper()));
            if (dev == null) {
                return;
            }
            var options = new PeripheralConnectionOptions();
            options.NotifyOnConnection = true;
            options.NotifyOnDisconnection = true;
            options.NotifyOnNotification = true;

            centralManager.ConnectPeripheral(dev, options);
        }

        public override void Disconnect() {
            if (IsConnected) {
                centralManager.CancelPeripheralConnection(connectedPeripheral);
                Services.Clear();
                Characteristics.Clear();
                Descriptors.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                connectedPeripheral = null;
                IsConnected = false;
            }
        }

        public override void SubscribeToCharacteristic(string characteristic, bool subscribe = true) {
            var c = getCharacteristic(CBUUID.FromString(characteristic));
            if (c != null && connectedPeripheral != null) {
                connectedPeripheral.SetNotifyValue(subscribe, c);
            }
        }

        public override void WriteCharacteristic(string characteristic, byte[] data) {
            var c = getCharacteristic(CBUUID.FromString(characteristic));
            if (c != null && connectedPeripheral != null) {
                connectedPeripheral.WriteValue(NSData.FromArray(data), c, CBCharacteristicWriteType.WithResponse);
            }
        }

        public override void ReadCharacteristic(string characteristic) {
            var c = getCharacteristic(CBUUID.FromString(characteristic));
            if (c != null && connectedPeripheral != null) {
                connectedPeripheral.ReadValue(c);
            }
        }

        public override void WriteDescriptor(string descriptor, byte[] data) {
            var desc = getDescriptor(CBUUID.FromString(descriptor));
            if (desc != null && connectedPeripheral != null) {
                connectedPeripheral.WriteValue(NSData.FromArray(data), desc);
            }
        }

        public override void ReadDescriptor(string descriptor) {
            var desc = getDescriptor(CBUUID.FromString(descriptor));
            if (desc != null && connectedPeripheral != null) {
                connectedPeripheral.ReadValue(desc);
            }
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

        /*
         * Get a desc/char/service from a UUID
         */
        private BLEDescriptor getDescriptor(CBUUID uuid) {
            return descriptorObjects.FirstOrDefault((item) => item.UUID.Equals(uuid));
        }
        private BLECharacteristic getCharacteristic(CBUUID uuid) {
            return characteristicObjects.FirstOrDefault((item) => item.UUID.Equals(uuid));
        }
        private CBService getService(CBUUID uuid) {
            return serviceObjects.FirstOrDefault((item) => item.UUID.Equals(uuid));
        }

        /**
         Expand UUID if it is only 4 chars (stick it into BLE Base UUID)
        */
        private string expand(string uuidString){
            if(uuidString.Length == 4){
                // Add the BLE Base UUID
                return "0000" + uuidString + "-0000-1000-8000-00805F9B34FB";
            }
            return uuidString;
        }

        private MyCentralmanagerDelegate cmDelegate;
        class MyCentralmanagerDelegate : CBCentralManagerDelegate {
            private IOSBLEClient client;

            public MyCentralmanagerDelegate(IOSBLEClient client) {
                this.client = client;
            }

            public override void UpdatedState(CBCentralManager central) {
                var result = central.State == CBCentralManagerState.PoweredOn;
                if (!result) {
                    client.StopScanning();
                    client.Disconnect();
                }
                client.Delegate.OnBluetoothPowerChanged(result);
            }
            public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral) {
                peripheral.Delegate = client.peripheralDelegate;
                peripheral.DiscoverServices(null);
                client.connectedPeripheral = peripheral;
                client.Services.Clear();
                client.Characteristics.Clear();
                client.Descriptors.Clear();
                client.Delegate.OnConnectToDevice(peripheral.Identifier.AsString().ToUpper(), peripheral.Name, true);
            }
            public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error) {
                client.Delegate.OnConnectToDevice(address: peripheral.Identifier.AsString().ToUpper(), name: peripheral.Name, success: false);
            }
            public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error) {
                client.Delegate.OnDisconnectFromDevice(address: peripheral.Identifier.AsString().ToUpper(), name: peripheral.Name);
            }
            public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI) {
                if (!client.deviceAddresses.Contains(peripheral.Identifier.AsString().ToUpper())) {
                    client.devices.Add(peripheral);
                    client.deviceAddresses.Add(peripheral.Identifier.AsString().ToUpper());
                }
                client.Delegate.OnDeviceDiscovered(address: peripheral.Identifier.AsString().ToUpper(), name: peripheral.Name, rssi: RSSI.Int32Value);
            }
        }

        // Keep track of how many responses there should be and have been for discovering chars/incServices/descs
        // This is Apple Specific
        private int charCallCount = 0;
        private int incCallCount = 0;
        private int descCallCount = 0;

        private int completedChar = 0;
        private int completedInc = 0;
        private int completedDesc = 0;

        private MyCBPeripheralDelegate peripheralDelegate;
        class MyCBPeripheralDelegate : CBPeripheralDelegate {
            private IOSBLEClient client;
            public MyCBPeripheralDelegate(IOSBLEClient client) {
                this.client = client;
            }

            public override void DiscoveredService(CBPeripheral peripheral, NSError error) {
                peripheral.Delegate = this;
                if (peripheral.Services != null) {
                    client.serviceObjects.AddRange(peripheral.Services);
                    client.charCallCount = client.serviceObjects.Count;
                    client.incCallCount = client.serviceObjects.Count;
                    client.descCallCount = 0;
                    foreach(var service in peripheral.Services){
                        client.Services.Add(client.expand(service.UUID.Uuid));
                        peripheral.DiscoverIncludedServices(null, service);
                        peripheral.DiscoverCharacteristics(null, service);
                    }
                }
            }
            public override void DiscoveredIncludedService(CBPeripheral peripheral, CBService service, NSError error) {
                client.completedInc += 1;
                peripheral.Delegate = this;
                if (service.IncludedServices != null) {
                    client.serviceObjects.AddRange(service.IncludedServices);
                    client.charCallCount += service.IncludedServices.Count();
                    foreach(var s in service.IncludedServices){
                        client.Services.Add(client.expand(s.UUID.ToString()));
                        peripheral.DiscoverCharacteristics(null, s);
                    }
                }
                client.allDiscovered();
            }
            public override void DiscoveredCharacteristic(CBPeripheral peripheral, CBService service, NSError error) {
                client.completedChar += 1;
                peripheral.Delegate = this;
                if (service.Characteristics != null) {
                    client.descCallCount += service.Characteristics.Count();
                    foreach(var characteristic in service.Characteristics){
                        client.characteristicObjects.Add(new BLECharacteristic(characteristic));
                        client.Characteristics.Add(client.expand(uuidString: characteristic.UUID.ToString()));
                        peripheral.DiscoverDescriptors(characteristic) ;
                    }
                }
                client.allDiscovered();
            }

            public override void DiscoveredDescriptor(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) {
                client.completedDesc += 1;
                if (characteristic.Descriptors != null) {
                    foreach (var descriptor in characteristic.Descriptors) {
                        client.descriptorObjects.Add(new BLEDescriptor(descriptor));
                        client.Descriptors.Add(client.expand(uuidString: descriptor.UUID.ToString()));
                    }
                }
                client.allDiscovered();
            }

            public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError error) {
                client.Delegate.OnDescriptorRead(client.expand(descriptor.UUID.ToString()).ToUpper(), error == null, ((NSData)descriptor.Value).ToArray());
            }

            public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) {
                client.Delegate.OnCharacteristicRead(client.expand(characteristic.UUID.ToString()).ToUpper(), error == null, characteristic.Value.ToArray());
            }

            public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) {
                client.Delegate.OnCharacteristicWrite(client.expand(characteristic.UUID.ToString()).ToUpper(), error == null, characteristic.Value.ToArray());
            }


            public override void WroteDescriptorValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError error) {
                client.Delegate.OnDescriptorWrite(client.expand(descriptor.UUID.ToString()).ToUpper(), error == null, value: ((NSData)descriptor.Value).ToArray());
            }

        }

        // Check if all services chars and descs have been discovered
        // All chars must be discovered before the client can subscribe to notifications (this is solution on iOS and macOS)
        private void allDiscovered() {
            if (charCallCount <= completedChar && incCallCount <= completedInc && descCallCount <= completedDesc ){
                Delegate.OnServicesDiscovered();
            }
        }



    }
}