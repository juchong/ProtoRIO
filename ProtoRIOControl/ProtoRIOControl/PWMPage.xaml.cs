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

        void pwmaChanged(object sender, EventArgs e){
            
        }
        void pwmbChanged(object sender, EventArgs e) {

        }
    }
}