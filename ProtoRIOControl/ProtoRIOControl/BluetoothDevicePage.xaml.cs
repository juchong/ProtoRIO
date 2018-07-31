﻿using System;
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
            MainPage.bluetooth.enumerateDevices(); // This MUST be called here or the callback will not work on Android
        }

        private void onCancelClicked(object sender, EventArgs e) {
            MainPage.bluetooth.endEnumeration();
            Navigation.PopModalAsync();
        }

        private void onDeviceSelected(object sender, SelectedItemChangedEventArgs e) {
            MainPage.bluetooth.endEnumeration();
            MainPage.bluetooth.connect(MainPage.discoveredDevices[MainPage.deviceNames.IndexOf((string)e.SelectedItem)]);
            Navigation.PopModalAsync();
        }
    }
}