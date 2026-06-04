namespace Collectary.Presentation.Services;

/// <summary>
/// Resolves the root directory for the app's local data (database, images, preferences, logs, token
/// caches). In <c>DEBUG</c> this sits next to the build output so each git worktree / running instance
/// is isolated — preventing the shared <c>%APPDATA%</c> database from being corrupted by builds with
/// different models. <c>RELEASE</c> uses the normal per-user <c>%APPDATA%\Collectary</c> location.
/// </summary>
public static class AppDataPaths
{
    public static string Root { get; } = Resolve();

    private static string Resolve()
    {
#if DEBUG
        return Path.Combine(AppContext.BaseDirectory, "collectary-data");
#else
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Collectary");
#endif
    }
}
