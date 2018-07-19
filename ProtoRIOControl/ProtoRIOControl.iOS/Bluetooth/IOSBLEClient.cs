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
            throw new NotImplementedException();
        }

        public override BtError CheckBluetooth() {
            throw new NotImplementedException();
        }

        public override void RequestEnableBt() {
            throw new NotImplementedException();
        }

        public override BtError ScanForDevices() {
            throw new NotImplementedException();
        }

        public override void StopScanning() {
            throw new NotImplementedException();
        }

        public override void ConnectToDevice(string deviceAddress) {
            throw new NotImplementedException();
        }

        public override void Disconnect() {
            throw new NotImplementedException();
        }

        public override void SubscribeToCharacteristic(string characteristic, bool subscribe = true) {
            throw new NotImplementedException();
        }

        public override void WriteCharacteristic(string characteristic, byte[] data) {
            throw new NotImplementedException();
        }

        public override void ReadCharacteristic(string characteristic) {
            throw new NotImplementedException();
        }

        public override void WriteDescriptor(string descriptor, byte[] data) {
            throw new NotImplementedException();
        }

        public override void ReadDescriptor(string descriptor) {
            throw new NotImplementedException();
        }

        public override bool HasService(string service) {
            throw new NotImplementedException();
        }

        public override bool HasCharacteristic(string characteristic) {
            throw new NotImplementedException();
        }

        public override bool HasDescriptor(string descriptor) {
            throw new NotImplementedException();
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