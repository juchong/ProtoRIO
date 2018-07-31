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

    public class IOSBluetooth : IBluetooth {

        // Platform Specific Objects
        private List<CBService> serviceObjects = new List<CBService>();
        private List<CBCharacteristic> characteristicObjects = new List<CBCharacteristic>();
        private List<CBDescriptor> descriptorObjects = new List<CBDescriptor>();
        private List<string> scanServices = new List<string>();

        private bool isScanning = false;
        private bool _isConnected = false;

        BTCallback callback;

        //Apple Specific Bluetooth Objects
        private CBCentralManager centralManager;
        private CBPeripheral connectedPeripheral;

        // keep track of discovered devices
        private List<string> deviceAddresses = new List<string>();
        private List<CBPeripheral> devices = new List<CBPeripheral>();

        public IOSBluetooth(BTCallback btCallback) {
            cmDelegate = new MyCentralmanagerDelegate(this);
            peripheralDelegate = new MyCBPeripheralDelegate(this);
            callback = btCallback;
            CBCentralInitOptions opts = new CBCentralInitOptions();
            opts.ShowPowerAlert = false;
            centralManager = new CBCentralManager(cmDelegate, null, opts);
        }

        public void scanForService(string service) {
            if (scanServices.Contains(service.ToUpper())) {
                scanServices.Add(service.ToUpper());
            }
        }

        public BtError checkBtSupport() {
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

        public void showEnableBtPrompt(string title, string message, string confirmText, string cancelText) {
            var vc = UIApplication.SharedApplication.KeyWindow.RootViewController;
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            var cancelAction = UIAlertAction.Create(cancelText, UIAlertActionStyle.Cancel, (action) => {
                alert.DismissViewController(true, null);
            });
            var settingsAction = UIAlertAction.Create(confirmText, UIAlertActionStyle.Default, (Action) => {
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

        public BtError enumerateDevices() {
            if (!isScanning) {
                devices.Clear();
                deviceAddresses.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                var error = checkBtSupport();
                if (error != BtError.None) {
                        return error;
                }
                var uuids = new List<CBUUID>();
                foreach(var s in scanServices){
                    uuids.Add(CBUUID.FromString(s));
                }
                var options = new PeripheralScanningOptions();
                options.AllowDuplicatesKey = true;
                centralManager.ScanForPeripherals(uuids.ToArray(), options);
                isScanning = true;
                return BtError.None;
            }else{
                return BtError.AlreadyRunning;
            }
        }

        public void endEnumeration() {
            if (isScanning) {
                centralManager.StopScan();
                isScanning = false;
            }
        }

        public void connect(string deviceAddress) {
            if (connectedPeripheral != null) {
                disconnect();
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

        public void disconnect() {
            if (_isConnected) {
                centralManager.CancelPeripheralConnection(connectedPeripheral);
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                connectedPeripheral = null;
                _isConnected = false;
            }
        }

        public void subscribeToUartChars() {
            var tx = getCharacteristic(CBUUID.FromString(BTValues.txCharacteristic));
            if (tx != null && connectedPeripheral != null) {
                connectedPeripheral.SetNotifyValue(true, tx);
            }
        }

        public void writeToUart(byte[] data) {
            var c = getCharacteristic(CBUUID.FromString(BTValues.rxCharacteristic));
            if (c != null && connectedPeripheral != null) {
                connectedPeripheral.WriteValue(NSData.FromArray(data), c, CBCharacteristicWriteType.WithoutResponse);
                callback.onUartDataSent(data);
            }
        }

        public bool hasUartService() {
            foreach (CBService s in serviceObjects) {
                if (s.UUID.Uuid.ToUpper().Equals(BTValues.uartService.ToUpper()))
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

        public void cancelConnect(){
            if(connectedPeripheral != null){
                centralManager.CancelPeripheralConnection(connectedPeripheral);
            }
        }

        /*
         * Get a desc/char/service from a UUID
         */
        private CBDescriptor getDescriptor(CBUUID uuid) {
            return descriptorObjects.FirstOrDefault((item) => item.UUID.Equals(uuid));
        }
        private CBCharacteristic getCharacteristic(CBUUID uuid) {
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
            private IOSBluetooth client;

            public MyCentralmanagerDelegate(IOSBluetooth client) {
                this.client = client;
            }

            public override void UpdatedState(CBCentralManager central) {
                var result = central.State == CBCentralManagerState.PoweredOn;
                if (!result) {
                    client.endEnumeration();
                    client.disconnect();
                }
                client.callback.onBluetoothPowerChanged(result);
            }
            public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral) {
                peripheral.Delegate = client.peripheralDelegate;
                peripheral.DiscoverServices(null);
                client.connectedPeripheral = peripheral;
                client.serviceObjects.Clear();
                client.characteristicObjects.Clear();
                client.descriptorObjects.Clear();
            }
            public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error) {
                client.callback.onConnectToDevice(address: peripheral.Identifier.AsString().ToUpper(), name: peripheral.Name, success: false);
            }
            public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error) {
                client.callback.onDisconnectFromDevice(address: peripheral.Identifier.AsString().ToUpper(), name: peripheral.Name);
            }
            public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI) {
                if (!client.deviceAddresses.Contains(peripheral.Identifier.AsString().ToUpper())) {
                    client.devices.Add(peripheral);
                    client.deviceAddresses.Add(peripheral.Identifier.AsString().ToUpper());
                }
                client.callback.onDeviceDiscovered(address: peripheral.Identifier.AsString().ToUpper(), name: peripheral.Name, rssi: RSSI.Int32Value);
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
            private IOSBluetooth client;
            public MyCBPeripheralDelegate(IOSBluetooth client) {
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
                        client.characteristicObjects.Add(characteristic);
                        peripheral.DiscoverDescriptors(characteristic) ;
                    }
                }
                client.allDiscovered();
            }

            public override void DiscoveredDescriptor(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) {
                client.completedDesc += 1;
                if (characteristic.Descriptors != null) {
                    foreach (var descriptor in characteristic.Descriptors) {
                        client.descriptorObjects.Add(descriptor);
                    }
                }
                client.allDiscovered();
            }

            public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) {
                if (characteristic.UUID.Uuid.ToUpper().Equals(BTValues.txCharacteristic.ToUpper()) && error == null){
                    byte[] value = new byte[]{0};
                    if (characteristic.Value != null)
                        value = characteristic.Value.ToArray();
                    client.callback.onUartDataReceived(value);
                }
            }
        }

        // Check if all services chars and descs have been discovered
        // All chars must be discovered before the client can subscribe to notifications (this is solution on iOS and macOS)
        private void allDiscovered() {
            if (charCallCount <= completedChar && incCallCount <= completedInc && descCallCount <= completedDesc) {
                callback.onConnectToDevice(connectedPeripheral.Identifier.AsString().ToUpper(), connectedPeripheral.Name, true);
            }
        }

    }
}