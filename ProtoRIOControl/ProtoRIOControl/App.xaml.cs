using ProtoRIOControl.Localization;
using System;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace ProtoRIOControl {
    public partial class App : Application {
        public App() {
            InitializeComponent();

            // This lookup NOT required for Windows platforms - the Culture will be automatically set
            if (Device.RuntimePlatform == Device.iOS || Device.RuntimePlatform == Device.Android) {
                // determine the correct, supported .NET culture
                var ci = DependencyService.Get<ILocalize>().GetCurrentCultureInfo();
                Localization.AppResources.Culture = ci; // set the RESX for resource localization
                DependencyService.Get<ILocalize>().SetLocale(ci); // set the Thread for locale-aware methods
            }

            MainPage = new NavigationPage(new MainPage());
        }

        protected override void OnStart() {
            // Handle when your app starts
        }

        protected override void OnSleep() {
            ProtoRIOControl.MainPage.bluetooth.endEnumeration();
            ProtoRIOControl.MainPage.bluetooth.disconnect();
            Device.BeginInvokeOnMainThread(() => ProtoRIOControl.MainPage.instance.statusPage.setStatusLabel(AppResources.StatusNotConnected, false));
        }

        protected override void OnResume() {
            // Handle when your app resumes
        }
    }
}
