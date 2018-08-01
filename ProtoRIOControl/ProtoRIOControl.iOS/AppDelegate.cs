using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using ProtoRIO.Bluetooth;
using ProtoRIOControl.iOS.Bluetooth;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace ProtoRIOControl.iOS {
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options) {

            MainPage.bluetooth = new IOSBluetooth(MainPage.btCallback);

            // Apply our theme
            UIColor tintColor = Xamarin.Forms.Color.FromHex("#009FBD").ToUIColor();

            // This color must be set to each element individualy
            UIButton.Appearance.TintColor = tintColor;
            UIButton.Appearance.SetTitleColor(tintColor, UIControlState.Normal);

            UISlider.Appearance.ThumbTintColor = tintColor;
            UISlider.Appearance.MinimumTrackTintColor = tintColor;
            UISlider.Appearance.MaximumTrackTintColor = tintColor;

            UINavigationBar.Appearance.TintColor = tintColor;
            UITabBar.Appearance.TintColor = tintColor;

            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }
    }
}
