using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Collectary.Presentation.ViewModels;
using Microsoft.Identity.Client;

namespace Collectary.UI.Android;

[Activity(
#if DEBUG
    Label = "DEBUG Collectary",
#else
    Label = "Collectary",
#endif
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private bool _backWired;

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
        WireSystemBack();
    }

    // When MSAL's sign-in browser returns, its activity finishes with a result that has to be handed
    // back to MSAL to complete the interactive token request; without this the sign-in silently ends
    // as cancelled and the app stays disconnected.
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }

    // Avalonia owns the Android system-back gesture and raises TopLevel.BackRequested; an Activity-level
    // OnBackPressed override never wins against it. We claim that event, mark it handled so Avalonia
    // performs no default, and route it through the shared navigation host instead.
    private void WireSystemBack()
    {
        if (_backWired) return;
        if (ResolveTopLevel() is not { } topLevel)
        {
            Dispatcher.UIThread.Post(WireSystemBack);
            return;
        }

        topLevel.BackRequested += OnBackRequested;
        _backWired = true;
    }

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (MainViewModel() is not { } vm)
        {
            Finish();
            return;
        }

        Dispatcher.UIThread.Post(async () =>
        {
            if (!await vm.HandleSystemBackAsync())
                Finish();
        });
    }

    private TopLevel? ResolveTopLevel() =>
        (Avalonia.Application.Current?.ApplicationLifetime as ISingleViewApplicationLifetime)?.MainView is { } view
            ? TopLevel.GetTopLevel(view)
            : null;

    private MainWindowViewModel? MainViewModel() =>
        (Avalonia.Application.Current?.ApplicationLifetime as ISingleViewApplicationLifetime)
            ?.MainView?.DataContext as MainWindowViewModel;
}
