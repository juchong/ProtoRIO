using ProtoRIOControl.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ProtoRIOControl {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PneumaticsPage : ContentPage {

        bool aIsOn = false;
        bool bIsOn = false;

        public PneumaticsPage() {
            InitializeComponent();
        }

        public void disableSolenoids() {
            aIsOn = false;
            bIsOn = false;
            MainPage.sendSolenoid(aIsOn, bIsOn);
            changeButtonState(solenoidAButton, aIsOn);
            changeButtonState(solenoidBButton, bIsOn);
            solenoidAButton.IsEnabled = false;
            solenoidBButton.IsEnabled = false;
            allowBothSwitch.IsEnabled = false;
            allowBothSwitch.IsToggled = false;
        }

        public void enableSolenoids() {
            solenoidAButton.IsEnabled = true;
            solenoidBButton.IsEnabled = true;
            allowBothSwitch.IsEnabled = true;
            allowBothSwitch.IsToggled = false;
            aIsOn = false;
            bIsOn = false;
            MainPage.sendSolenoid(aIsOn, bIsOn);
        }

        void buttonClicked(object sender, EventArgs e) {
            if(sender == solenoidAButton) {
                // If A is off (and being turned on) and B is already on and both are not allowed toggle both
                if(!aIsOn && bIsOn && !allowBothSwitch.IsToggled) {
                    aIsOn = !aIsOn;
                    bIsOn = !bIsOn;
                    changeButtonState(solenoidAButton, aIsOn);
                    changeButtonState(solenoidBButton, bIsOn);
                } 
                // Otherwise just toggle A (A could be being turned off, or B may already be off, or both are allowed)
                else {
                    aIsOn = !aIsOn;
                    changeButtonState(solenoidAButton, aIsOn);
                }
            }else if (sender == solenoidBButton) {
                // If B is off (and being turned on) and A is already on and both are not allowed toggle both
                if (!bIsOn && aIsOn && !allowBothSwitch.IsToggled) {
                    aIsOn = !aIsOn;
                    bIsOn = !bIsOn;
                    changeButtonState(solenoidAButton, aIsOn);
                    changeButtonState(solenoidBButton, bIsOn);
                }
                // Otherwise just toggle B (B could be being turned off, or A may already be off, or both are allowed)
                else {
                    bIsOn = !bIsOn;
                    changeButtonState(solenoidBButton, bIsOn);
                }
            }
            MainPage.sendSolenoid(aIsOn, bIsOn);
        }
    
        void changeButtonState(Button b, bool isOn) {
            if(isOn) {
                // Turn on
                b.Text = AppResources.On;
                b.BackgroundColor = (Color)Application.Current.Resources["onColor"];
            } else {
                // Turn off
                b.Text = AppResources.Off;
                b.BackgroundColor = (Color)Application.Current.Resources["offColor"];
            }
        }

        private void allowBothToggled(object sender, ToggledEventArgs e) {
            // If both are no longer allowed but both are on turn both off
            if(e.Value == false && aIsOn && bIsOn) {
                aIsOn = false;
                bIsOn = false;
                MainPage.sendSolenoid(aIsOn, bIsOn);
                changeButtonState(solenoidAButton, aIsOn);
                changeButtonState(solenoidBButton, bIsOn);
            }
        }
    }
}