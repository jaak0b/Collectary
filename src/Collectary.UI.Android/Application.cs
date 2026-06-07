using Android.App;
using Android.Runtime;
using Autofac.Core;
using Avalonia;
using Avalonia.Android;
using Collectary.Infrastructure.Cloud;
using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Presentation.Services;

namespace Collectary.UI.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<global::Collectary.UI.App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        /// <summary>The foreground Activity, published by <see cref="MainActivity"/>, so the OneDrive
        /// MSAL sign-in can launch its Chrome Custom Tab against it.</summary>
        public Activity? CurrentActivity { get; set; }

        /// <summary>Coordinates runtime permission prompts: the DI service starts a request and
        /// <see cref="MainActivity"/> routes the result back here so the awaiting feature resumes.</summary>
        public Permissions.AndroidPermissionCoordinator PermissionCoordinator { get; } = new();

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .AfterSetup(RegisterCloudModule);
        }

        // Mirrors the desktop head's wiring, but with Android MSAL options: a custom-scheme redirect
        // caught by the manifest's BrowserTabActivity, the current Activity as the interactive parent,
        // and no desktop token-cache helper (MSAL uses its built-in Keystore-backed cache on Android).
        private void RegisterCloudModule(AppBuilder builder)
        {
            if (builder.Instance is not global::Collectary.UI.App app) return;

            var cacheDirectory = AppDataPaths.Root;
            var androidConfig = new AndroidCloudConfig();
            var oneDriveMsalOptions = new AndroidMsalPlatformOptionsFactory(
                AndroidCloudConfig.PackageName,
                androidConfig.SignatureHash,
                () => CurrentActivity).Create();

            app.PlatformModules = new IModule[]
            {
                new CloudModule(
                    cacheDirectory,
                    oneDriveMsalOptions,
                    () => AppPreferences.Load().OneDriveRootFolderId,
                    () => AppPreferences.Load().GoogleDriveRootFolderId,
                    androidConfig.OneDriveClientId),
                new Audio.AndroidAudioModule(),
                new Camera.AndroidCameraModule(),
                new Permissions.AndroidPermissionsModule(),
            };
        }
    }
}
