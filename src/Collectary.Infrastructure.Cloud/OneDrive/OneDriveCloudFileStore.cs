using Collectary.Core.Ports;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Collectary.Infrastructure.Cloud.OneDrive;

/// <summary>
/// <see cref="ICloudFileStore"/> backed by OneDrive via Microsoft Graph. The sync root is a folder
/// the user picked (its Graph item id), under which <c>CloudSyncBackend</c> creates the per-kind
/// subfolders.
/// </summary>
public class OneDriveCloudFileStore : ICloudFileStore, ICloudRootProvider
{
    // Graph "simple upload" supports up to 250 MB, but Microsoft recommends an upload session
    // beyond ~4 MB for reliability/resumability.
    private const int DefaultLargeUploadThreshold = 4 * 1024 * 1024;
    private const int UploadSliceSize = 5 * 320 * 1024; // must be a multiple of 320 KiB

    private readonly GraphServiceClient _graph;
    private readonly ICloudAuthClient _auth;
    private readonly Func<string?> _rootFolderIdProvider;
    private readonly int _largeUploadThreshold;
    private string? _driveId;

    public OneDriveCloudFileStore(
        GraphServiceClient graph,
        ICloudAuthClient auth,
        Func<string?> rootFolderIdProvider,
        int largeUploadThreshold = DefaultLargeUploadThreshold)
    {
        _graph = graph;
        _auth = auth;
        _rootFolderIdProvider = rootFolderIdProvider;
        _largeUploadThreshold = largeUploadThreshold;
    }

    public bool IsAvailable => _auth.IsSignedIn && !string.IsNullOrWhiteSpace(_rootFolderIdProvider());

    public string RootFolderId => _rootFolderIdProvider() ?? string.Empty;

    public async Task<string> EnsureFolderAsync(string parentFolderId, string name, CancellationToken ct)
    {
        var existing = (await ChildrenAsync(parentFolderId, ct))
            .FirstOrDefault(i => i.Folder is not null && string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing.Id!;

        var driveId = await DriveIdAsync(ct);
        var created = await _graph.Drives[driveId].Items[parentFolderId].Children.PostAsync(new DriveItem
        {
            Name = name,
            Folder = new Folder(),
            AdditionalData = { ["@microsoft.graph.conflictBehavior"] = "fail" },
        }, cancellationToken: ct);

        return created!.Id!;
    }

    public async Task<IReadOnlyList<CloudFile>> ListFilesAsync(string folderId, CancellationToken ct) =>
        (await ChildrenAsync(folderId, ct))
            .Where(i => i.Folder is null)
            .Select(i => new CloudFile(i.Id!, i.Name!, i.Size ?? 0))
            .ToList();

    public async Task<IReadOnlyList<CloudFolder>> ListFoldersAsync(string folderId, CancellationToken ct) =>
        (await ChildrenAsync(folderId, ct))
            .Where(i => i.Folder is not null)
            .Select(i => new CloudFolder(i.Id!, i.Name!))
            .ToList();

    public async Task<byte[]?> DownloadAsync(string folderId, string name, CancellationToken ct)
    {
        var child = (await ChildrenAsync(folderId, ct)).FirstOrDefault(i => i.Name == name);
        if (child is null) return null;

        var driveId = await DriveIdAsync(ct);
        var stream = await _graph.Drives[driveId].Items[child.Id].Content.GetAsync(cancellationToken: ct);
        if (stream is null) return null;

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    public async Task UploadAsync(string folderId, string name, byte[] content, CancellationToken ct)
    {
        var driveId = await DriveIdAsync(ct);
        using var buffer = new MemoryStream(content);
        var item = _graph.Drives[driveId].Items[folderId].ItemWithPath(name);

        if (content.Length <= _largeUploadThreshold)
        {
            await item.Content.PutAsync(buffer, cancellationToken: ct);
            return;
        }

        var body = new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody
        {
            Item = new DriveItemUploadableProperties
            {
                AdditionalData = { ["@microsoft.graph.conflictBehavior"] = "replace" },
            },
        };
        var session = await item.CreateUploadSession.PostAsync(body, cancellationToken: ct);
        var uploadTask = new LargeFileUploadTask<DriveItem>(session, buffer, UploadSliceSize, _graph.RequestAdapter);
        await uploadTask.UploadAsync();
    }

    public async Task DeleteAsync(string folderId, string name, CancellationToken ct)
    {
        var child = (await ChildrenAsync(folderId, ct)).FirstOrDefault(i => i.Name == name);
        if (child is null) return;

        var driveId = await DriveIdAsync(ct);
        await _graph.Drives[driveId].Items[child.Id].DeleteAsync(cancellationToken: ct);
    }

    public async Task<CloudFolder> GetRootFolderAsync(CancellationToken ct)
    {
        var driveId = await DriveIdAsync(ct);
        var root = await _graph.Drives[driveId].Items["root"].GetAsync(cancellationToken: ct);
        var id = root?.Id ?? throw new InvalidOperationException("Unable to resolve the OneDrive root folder.");
        return new CloudFolder(id, root!.Name ?? "OneDrive");
    }

    private async Task<IReadOnlyList<DriveItem>> ChildrenAsync(string folderId, CancellationToken ct)
    {
        var driveId = await DriveIdAsync(ct);
        var children = _graph.Drives[driveId].Items[folderId].Children;

        var items = new List<DriveItem>();
        var response = await children.GetAsync(cancellationToken: ct);
        while (response?.Value is not null)
        {
            items.AddRange(response.Value);
            if (string.IsNullOrEmpty(response.OdataNextLink)) break;
            response = await children.WithUrl(response.OdataNextLink).GetAsync(cancellationToken: ct);
        }
        return items;
    }

    private async Task<string> DriveIdAsync(CancellationToken ct)
    {
        if (_driveId is not null) return _driveId;
        var drive = await _graph.Me.Drive.GetAsync(cancellationToken: ct);
        _driveId = drive?.Id ?? throw new InvalidOperationException("Unable to resolve the OneDrive drive id.");
        return _driveId;
    }
}
