using ProtoRIO.Bluetooth;
using ProtoRIOControl.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ProtoRIOControl {
    public partial class MainPage : TabbedPage{

        public static IBluetooth bluetooth;
        public static MyBtCallback btCallback = new MyBtCallback();

        public MainPage() {
            InitializeComponent();
        }

        void OnConnectClicked(object src, EventArgs e) {

        }

        public class MyBtCallback : BTCallback {
            // Connection events
            public void onDeviceDiscovered(string address, string name, int rssi) {

            }
            public void onConnectToDevice(string address, string name, bool success) {

            }
            public void onDisconnectFromDevice(string address, string name) {

            }

            // Data events
            public void onUartDataReceived(byte[] data) {

            }
            public void onUartDataSent(byte[] value, bool success) {

            }
            public void onBluetoothPowerChanged(bool enabled) {

            }
        }
    }
}
