using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android;
using System.Collections.Generic;
using Android.Support.V4.Content;
using Android.Support.V4.App;

namespace ProtoRIOControl.Droid {
    [Activity(Label = "ProtoRIOControl", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {
        public static Context MainContext;
        protected override void OnCreate(Bundle bundle) {

            setupPermissions();

            MainContext = this;
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }

        // Android Permissions
        String[] permissions = new String[] { Manifest.Permission.AccessCoarseLocation };
        String[] permissionMessages = new String[] { "Location access is required to use Bluetooth Low Energy Devices."};
        int REQUEST_PERMISSION_CODE = 2;

        // Permission stuff for android 26+
        private void setupPermissions() {
            List<string> requestPermissions = new List<string>();
            foreach (string permission in permissions) {
                Permission status = ContextCompat.CheckSelfPermission(this, permission);
                if (status != Permission.Granted)
                    requestPermissions.Add(permission);
            }
            if (requestPermissions.Count > 0)
                ActivityCompat.RequestPermissions(this, requestPermissions.ToArray(), REQUEST_PERMISSION_CODE);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults) {
            if (requestCode == REQUEST_PERMISSION_CODE) {
                foreach (Permission result in grantResults) {
                    if (result != Permission.Granted) {
                        AlertDialog.Builder builder = new AlertDialog.Builder(this);
                        builder.SetTitle("Lack of permissions.");
                        builder.SetMessage("The app may not function correctly without all permissions.");
                        builder.SetPositiveButton("OK", (sender, which) => { });
                        builder.Create().Show();
                    }
                }
            } else {
                // Not ours. Pass it up
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }

    }
}

