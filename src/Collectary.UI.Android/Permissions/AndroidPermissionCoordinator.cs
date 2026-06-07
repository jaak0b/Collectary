using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;

namespace Collectary.UI.Android.Permissions;

public sealed class AndroidPermissionCoordinator
{
    private readonly object _sync = new();
    private readonly Dictionary<int, TaskCompletionSource<bool>> _pending = new();
    private int _nextCode = 9000;

    public Task<bool> RequestAsync(Activity activity, string permission)
    {
        if (activity.CheckSelfPermission(permission) == Permission.Granted)
            return Task.FromResult(true);

        int code;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            code = _nextCode++;
            _pending[code] = tcs;
        }

        activity.RequestPermissions(new[] { permission }, code);
        return tcs.Task;
    }

    public void OnResult(int requestCode, Permission[] grantResults)
    {
        TaskCompletionSource<bool>? tcs;
        lock (_sync)
        {
            if (!_pending.Remove(requestCode, out tcs)) return;
        }

        var granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
        tcs.TrySetResult(granted);
    }
}
