namespace Collectary.Core.Ports;

/// <summary>
/// Resolves the top-level folder from which the user starts browsing for a sync folder
/// (e.g. the OneDrive drive root). Combined with <see cref="ICloudFileStore.ListFoldersAsync"/>
/// and <see cref="ICloudFileStore.EnsureFolderAsync"/>, this drives the cloud folder picker.
/// </summary>
public interface ICloudRootProvider
{
    Task<CloudFolder> GetRootFolderAsync(CancellationToken ct);
}
