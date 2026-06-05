using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace Collectary.UI.Android;

[Activity(
    Label = "Collectary.UI.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    // MSAL's interactive sign-in needs the foreground Activity to launch its Chrome Custom Tab.
    // Publish it to the Application so the cloud module's parent-activity provider can hand it to MSAL.
    protected override void OnResume()
    {
        base.OnResume();
        if (this.Application is Application app)
            app.CurrentActivity = this;
    }
}
