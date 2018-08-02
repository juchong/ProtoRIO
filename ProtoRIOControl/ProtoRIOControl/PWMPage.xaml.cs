using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ProtoRIOControl {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PWMPage : ContentPage {

        bool pwmaPercentMode = true;
        bool pwmbPercentMode = true;

        public PWMPage() {
            InitializeComponent();
        }

        public void disableAll(){
            pwmaSwitch.IsToggled = false;
            pwmbSwitch.IsToggled = false;
        }

        void buttonClicked(object sender, EventArgs e) {
            if (sender == pwmaNeutralButton)
                pwmaSlider.Value = (pwmaSlider.Maximum + pwmaSlider.Minimum) / 2;
            else if (sender == pwmaForwardButton)
                pwmaSlider.Value = pwmaSlider.Maximum;
            else if (sender == pwmaReverseButton)
                pwmaSlider.Value = pwmaSlider.Minimum;
            else if (sender == pwmbNeutralButton)
                pwmbSlider.Value = (pwmbSlider.Maximum + pwmbSlider.Minimum) / 2;
            else if (sender == pwmbForwardButton)
                pwmbSlider.Value = pwmbSlider.Maximum;
            else if (sender == pwmbReverseButton)
                pwmbSlider.Value = pwmbSlider.Minimum;
            else if (sender == pwmaModeButton) {

            }else if(sender == pwmbModeButton) {

            }
        }

        void pwmaToggled(object sender, ToggledEventArgs e) {
            // Do not allow enable if there is no connection
            if(pwmaSwitch.IsToggled && !MainPage.bluetooth.isConnected()){
                pwmaSwitch.IsToggled = false;
                return;
            }
            pwmaSlider.IsEnabled = pwmaSwitch.IsToggled;
            pwmaForwardButton.IsEnabled = pwmaSwitch.IsToggled;
            pwmaNeutralButton.IsEnabled = pwmaSwitch.IsToggled;
            pwmaReverseButton.IsEnabled = pwmaSwitch.IsToggled;
            pwmaModeButton.IsEnabled = pwmaSwitch.IsToggled;
            if(!pwmaSwitch.IsToggled){
                pwmaSlider.Value = (pwmaSlider.Maximum + pwmaSlider.Minimum) / 2;
            }else{
                MainPage.sendPWMA((pwmaSlider.Maximum + pwmaSlider.Minimum) / 2); // Resend
            }
        }
        void pwmaChanged(object sender, ValueChangedEventArgs e){
            if (e.NewValue % 1 != 0) {
                pwmaSlider.Value = Math.Round(e.NewValue);
                return;
            }
            MainPage.sendPWMA(e.NewValue);
        }

        void pwmbToggled(object sender, ToggledEventArgs e){
            // Do not allow enable if there is no connection
            if (pwmbSwitch.IsToggled && !MainPage.bluetooth.isConnected()) {
                pwmbSwitch.IsToggled = false;
                return;
            }
            pwmbSlider.IsEnabled = pwmbSwitch.IsToggled;
            pwmbForwardButton.IsEnabled = pwmbSwitch.IsToggled;
            pwmbNeutralButton.IsEnabled = pwmbSwitch.IsToggled;
            pwmbReverseButton.IsEnabled = pwmbSwitch.IsToggled;
            pwmbModeButton.IsEnabled = pwmbSwitch.IsToggled;
            if (!pwmbSwitch.IsToggled) {
                pwmbSlider.Value = (pwmbSlider.Maximum + pwmbSlider.Minimum) / 2;
            } else {
                MainPage.sendPWMB((pwmbSlider.Maximum + pwmbSlider.Minimum) / 2); // Resend
            }
        }
        void pwmbChanged(object sender, ValueChangedEventArgs e) {
            if(e.NewValue % 1 != 0) {
                pwmbSlider.Value = Math.Round(e.NewValue);
                return;
            }
            MainPage.sendPWMB(e.NewValue);
        }
    }
}