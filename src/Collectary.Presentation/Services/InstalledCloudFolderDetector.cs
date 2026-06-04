namespace Collectary.Presentation.Services;

/// <summary>
/// Detects a locally-mounted cloud folder (the OneDrive/Google Drive desktop client's synced folder)
/// so a desktop user can sync into it via the plain Folder backend — no API/OAuth required.
/// Returns null when none is found (e.g. the client isn't installed, or on mobile).
/// </summary>
public class InstalledCloudFolderDetector
{
    public string? Detect()
    {
        foreach (var variable in new[] { "OneDriveConsumer", "OneDrive" })
        {
            var path = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                return path;
        }

        return null;
    }
}
