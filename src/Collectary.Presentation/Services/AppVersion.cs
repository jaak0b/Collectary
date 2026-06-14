namespace Collectary.Presentation.Services;

public sealed class AppVersion
{
    public string Display { get; }

    public AppVersion(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            Display = "0.0.0";
            return;
        }

        var metadataStart = informationalVersion.IndexOf('+');
        Display = metadataStart >= 0
            ? informationalVersion[..metadataStart]
            : informationalVersion;
    }
}
