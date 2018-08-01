using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoRIO.Bluetooth;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ProtoRIOControl {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BluetoothDevicePage : ContentPage {
        public BluetoothDevicePage() {
            InitializeComponent();
            devicesList.ItemsSource = MainPage.deviceNames;
        }

        protected override void OnAppearing() {
            MainPage.bluetooth.enumerateDevices(); // On iOS this must be started after this page is visible or there will be ListView issues (invalid number of cells crashes)
        }

        private void onCancelClicked(object sender, EventArgs e) {
            Navigation.PopModalAsync();
        }

        private void onDeviceSelected(object sender, ItemTappedEventArgs e) {
            MainPage.deviceToConnectTo = MainPage.deviceNames.IndexOf((string)e.Item);
            ((ListView)sender).SelectedItem = null;
            Navigation.PopModalAsync();
        }
    }
}