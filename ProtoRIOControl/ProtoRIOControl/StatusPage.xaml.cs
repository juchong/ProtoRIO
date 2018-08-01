using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoRIOControl.Localization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ProtoRIOControl {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatusPage : ContentPage {
        
        double highBat = 11.5;
        double medBat = 10;

        public StatusPage() {
            InitializeComponent();
        }
        public void setStatusLabel(string text, bool isConnected) {
            connectionStatusLabel.Text = text;
            connectionStatusLabel.TextColor = isConnected ? ((Color)Application.Current.Resources["greenLabelColor"]) : ((Color)Application.Current.Resources["redLabelColor"]);
        }
        public void setBatteryInfo(string voltage, string current){
            try{
                double v = double.Parse(voltage);
                if (v >= highBat)
                    voltageLabel.BackgroundColor = (Color)Application.Current.Resources["batteryHighColor"];
                else if (v >= medBat)
                    voltageLabel.BackgroundColor = (Color)Application.Current.Resources["batteryMedColor"];
                else
                    voltageLabel.BackgroundColor = (Color)Application.Current.Resources["batteryLowColor"];
                voltageLabel.Text = String.Format("{0:0.00}", v) + " " + AppResources.VoltsSymbol;

                double c = double.Parse(current);
                currentLabel.Text = String.Format("{0:0.00}", c) + " " + AppResources.AmpsSymbol;
            }catch(Exception e){
                voltageLabel.BackgroundColor = Color.Transparent;
                voltageLabel.Text = "???";
                currentLabel.Text = "???";
            }
        }
    }
}