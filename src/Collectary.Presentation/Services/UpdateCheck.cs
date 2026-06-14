using Collectary.Core.Ports;

namespace Collectary.Presentation.Services;

public sealed class UpdateCheck
{
    private readonly IAppUpdater _updater;
    private readonly IAppLogger _logger;

    public UpdateCheck(IAppUpdater updater, IAppLogger logger)
    {
        _updater = updater;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        try
        {
            if (await _updater.CheckForUpdateAsync())
            {
                await _updater.DownloadUpdateAsync();
                _updater.ApplyUpdateOnExit();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Background update check failed.");
        }
    }
}
