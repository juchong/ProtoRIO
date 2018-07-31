using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ProtoRIOControl {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatusPage : ContentPage {
        public StatusPage() {
            InitializeComponent();
        }
        public void setStatusLabel(string text, Color textColor) {
            connectionStatusLabel.Text = text;
            connectionStatusLabel.TextColor = textColor;
        }
    }
}