using System.Threading.Tasks;
using Collectary.Presentation.Services;
using Velopack;
using Velopack.Sources;

namespace Collectary.UI.Desktop;

public sealed class VelopackAppUpdater : IAppUpdater
{
    private readonly UpdateManager _manager;
    private UpdateInfo? _pending;

    public VelopackAppUpdater()
        => _manager = new UpdateManager(new GithubSource("https://github.com/jaak0b/Collectary", null, false));

    public async Task<bool> CheckForUpdateAsync()
    {
        if (!_manager.IsInstalled) return false;

        _pending = await _manager.CheckForUpdatesAsync();
        return _pending is not null;
    }

    public async Task DownloadUpdateAsync()
    {
        if (_pending is not null)
            await _manager.DownloadUpdatesAsync(_pending);
    }

    public void ApplyUpdateOnExit()
    {
        if (_pending is not null)
            _manager.WaitExitThenApplyUpdates(_pending.TargetFullRelease, silent: true, restart: false);
    }
}
