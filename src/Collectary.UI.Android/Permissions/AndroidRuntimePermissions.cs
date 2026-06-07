using System.Threading.Tasks;
using Collectary.Core.Ports;
using AndroidApplication = Android.App.Application;

namespace Collectary.UI.Android.Permissions;

public sealed class AndroidRuntimePermissions : IRuntimePermissions
{
    public Task<bool> RequestAsync(RuntimePermission permission)
    {
        if (AndroidApplication.Context is not Collectary.UI.Android.Application app ||
            app.CurrentActivity is not { } activity)
            return Task.FromResult(false);

        var name = permission switch
        {
            RuntimePermission.Microphone => global::Android.Manifest.Permission.RecordAudio,
            RuntimePermission.Camera => global::Android.Manifest.Permission.Camera,
            _ => null
        };
        if (name is null) return Task.FromResult(false);

        return app.PermissionCoordinator.RequestAsync(activity, name);
    }
}
