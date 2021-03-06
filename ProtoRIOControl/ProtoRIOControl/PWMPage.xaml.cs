﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ProtoRIOControl {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PWMPage : ContentPage {

        public PWMPage() {
            InitializeComponent();
        }

        public void disableAll(){
            pwmaSwitch.IsToggled = false;
            pwmbSwitch.IsToggled = false;
            pwmaSwitch.IsEnabled = false;
            pwmbSwitch.IsEnabled = false;
        }

        public void enableAll() {
            pwmaSwitch.IsEnabled = true;
            pwmbSwitch.IsEnabled = true;
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
        }

        void pwmaToggled(object sender, ToggledEventArgs e) {
            pwmaSlider.IsEnabled = pwmaSwitch.IsToggled;
            pwmaForwardButton.IsEnabled = pwmaSwitch.IsToggled;
            pwmaNeutralButton.IsEnabled = pwmaSwitch.IsToggled;
            pwmaReverseButton.IsEnabled = pwmaSwitch.IsToggled;
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
            pwmbSlider.IsEnabled = pwmbSwitch.IsToggled;
            pwmbForwardButton.IsEnabled = pwmbSwitch.IsToggled;
            pwmbNeutralButton.IsEnabled = pwmbSwitch.IsToggled;
            pwmbReverseButton.IsEnabled = pwmbSwitch.IsToggled;
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