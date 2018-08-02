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
    public partial class SensorsPage : ContentPage {

        int sensorASelection = 0;
        int sensorBSelection = 0;

        public SensorsPage() {
            InitializeComponent();
        }
    }
}