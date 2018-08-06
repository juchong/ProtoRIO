using ProtoRIOControl.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ProtoRIOControl {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SensorsPage : ContentPage {

        public IList<Sensor> sensorASensors {
            get {
                return new List<Sensor>(new Sensor[] {
                    Sensors.none,
                    Sensors.encoder,
                    Sensors.hcsr04,
                    Sensors.mb1cm,
                    Sensors.mb2cm,
                    Sensors.sa60,
                    Sensors.sa02,
                    Sensors.sa21,
                    Sensors.sa41,
                    Sensors.sa51,
                    Sensors.analog
                });
            }
        }
        public IList<Sensor> sensorBSensors {
            get {
                return new List<Sensor>(new Sensor[] {
                    Sensors.none,
                    Sensors.hcsr04,
                    Sensors.mb1cm,
                    Sensors.mb2cm,
                    Sensors.sa60,
                    Sensors.sa02,
                    Sensors.sa21,
                    Sensors.sa41,
                    Sensors.sa51,
                    Sensors.analog,
                    Sensors.vl53l0xtof,
                    Sensors.lidarlite
                });
            }
        }

        public void disableAll() {
            sensorAPicker.SelectedIndex = 0;
            sensorBPicker.SelectedIndex = 0;
        }

        public void enableAll() {
            sensorAPicker.IsEnabled = true;
            sensorBPicker.IsEnabled = true;
        }

        /// <summary>
        /// Display the results that were sent from the ProtoRIO for sensor A
        /// </summary>
        /// <param name="result1">Result 1</param>
        /// <param name="result2">Result 2</param>
        public void setSensorAInfo(string result1, string result2) {
            // Update the data unless no sensor is selected
            if (sensorAPicker.SelectedItem != Sensors.none) {
                Sensor sensor = (Sensor)sensorAPicker.SelectedItem;
                sensorAResults1.Text = result1;
                sensorAResults2.Text = result2;
            }
        }

        private void sensorASelected(object sender, EventArgs e) {
            Sensor sensor = (Sensor)sensorAPicker.SelectedItem;
            MainPage.sendSensorASelection(sensor.sensorType);
            sensorAResults1.Text = "";
            sensorAResults2.Text = "";
            sensorAUnit1.Text = sensor.firstUnit;
            sensorAUnit2.Text = sensor.secondUnit;
            sensorASetting.Text = (sensor.setting == null) ? "" : (sensor.setting + "");
            sensorASettingUnit.Text = sensor.settingUnit;
            sensorASetting.IsEnabled = sensor.userSetting;
            sensorAConfig.Text = Sensors.getConnectionText(sensor.connection, true);
        }

        /// <summary>
        /// Display the results that were sent from the ProtoRIO for sensor B
        /// </summary>
        /// <param name="result1">Result 1</param>
        /// <param name="result2">Result 2</param>
        public void setSensorBInfo(string result1, string result2) {
            // Update the data unless no sensor is selected
            if (sensorBPicker.SelectedItem != Sensors.none) {
                Sensor sensor = (Sensor)sensorBPicker.SelectedItem;
                sensorBResults1.Text = result1;
                sensorBResults2.Text = result2;
            }
        }

        private void sensorBSelected(object sender, EventArgs e) {
            Sensor sensor = (Sensor)sensorBPicker.SelectedItem;
            MainPage.sendSensorBSelection(sensor.sensorType);
            sensorBResults1.Text = "";
            sensorBResults2.Text = "";
            sensorBUnit1.Text = sensor.firstUnit;
            sensorBUnit2.Text = sensor.secondUnit;
            sensorBSetting.Text = (sensor.setting == null) ? "" : (sensor.setting + "");
            sensorBSettingUnit.Text = sensor.settingUnit;
            sensorBSetting.IsEnabled = sensor.userSetting;
            sensorBConfig.Text = Sensors.getConnectionText(sensor.connection, false);
        }

        public SensorsPage() {
            InitializeComponent();
            BindingContext = this;
            sensorAPicker.SelectedIndex = 0;
            sensorBPicker.SelectedIndex = 0;
        }

        // These events are only fired when the user hits enter (this prevents issues of not knowing when the value is complete and from having the code driven changes fire this event)
        private void sensorASettingChanged(object sender, EventArgs e) {
            try {
                MainPage.sendSensorASetting(int.Parse(sensorASetting.Text));
            } catch (Exception ex) { }
        }
        private void sensorBSettingChanged(object sender, EventArgs e) {
            try {
                MainPage.sendSensorBSetting(int.Parse(sensorBSetting.Text));
            } catch (Exception ex) { }
        }
    }
}