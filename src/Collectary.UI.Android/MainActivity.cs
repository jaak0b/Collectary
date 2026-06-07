using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Android;

[Activity(
    Label = "Collectary.UI.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    // Microphone and camera permissions are requested lazily, the first time the user records or scans
    // (see AndroidPermissionCoordinator), so we don't prompt at launch for features they may never use.
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (this.Application is Application app)
            app.PermissionCoordinator.OnResult(requestCode, grantResults);
    }

    // MSAL's interactive sign-in needs the foreground Activity to launch its Chrome Custom Tab.
    // Publish it to the Application so the cloud module's parent-activity provider can hand it to MSAL.
    protected override void OnResume()
    {
        base.OnResume();
        if (this.Application is Application app)
            app.CurrentActivity = this;
    }

    // The phone's back gesture must not drop the user out of an in-progress edit. We route it through
    // the shared navigation host, which saves the current screen and steps back; only when there is
    // nothing left to step back to do we hand the gesture to the OS by sending the app to the background.
    public override void OnBackPressed()
    {
        if (MainViewModel() is not { } vm)
        {
            base.OnBackPressed();
            return;
        }

        Dispatcher.UIThread.Post(async () =>
        {
            if (!await vm.HandleSystemBackAsync())
                MoveTaskToBack(true);
        });
    }

    private MainWindowViewModel? MainViewModel() =>
        (Avalonia.Application.Current?.ApplicationLifetime as ISingleViewApplicationLifetime)
            ?.MainView?.DataContext as MainWindowViewModel;
}
