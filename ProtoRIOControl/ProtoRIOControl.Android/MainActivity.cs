using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;
using ProtoRIO.Bluetooth;
using ProtoRIOControl.Droid.Bluetooth;

namespace ProtoRIOControl.Droid {
    [Activity(Label = "ProtoRIOControl", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {

        public static Context MainContext;

        protected override void OnCreate(Bundle bundle) {
            MainContext = this;

            BLEClient.Builder = new AndroidBLEClientBuilder();

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }
    }
}

