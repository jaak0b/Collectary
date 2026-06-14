namespace Collectary.Presentation.Services;

public interface IAppUpdater
{
    Task<bool> CheckForUpdateAsync();

    Task DownloadUpdateAsync();

    void ApplyUpdateOnExit();
}
