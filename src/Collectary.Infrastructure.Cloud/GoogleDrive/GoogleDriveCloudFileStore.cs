using Collectary.Core.Ports;
using Google.Apis.Drive.v3;
using DriveData = Google.Apis.Drive.v3.Data;

namespace Collectary.Infrastructure.Cloud.GoogleDrive;

/// <summary>
/// <see cref="ICloudFileStore"/> backed by Google Drive. Uses the least-privilege <c>drive.file</c>
/// scope, so the app only sees files/folders it created — the sync root is therefore an app-owned
/// "Collectary" folder rather than the user's whole drive.
/// </summary>
public class GoogleDriveCloudFileStore : ICloudFileStore, ICloudRootProvider
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    private readonly DriveService _drive;
    private readonly ICloudAuthClient _auth;
    private readonly Func<string?> _rootFolderIdProvider;

    public GoogleDriveCloudFileStore(DriveService drive, ICloudAuthClient auth, Func<string?> rootFolderIdProvider)
    {
        _drive = drive;
        _auth = auth;
        _rootFolderIdProvider = rootFolderIdProvider;
    }

    public bool IsAvailable => _auth.IsSignedIn && !string.IsNullOrWhiteSpace(_rootFolderIdProvider());

    public string RootFolderId => _rootFolderIdProvider() ?? string.Empty;

    public async Task<CloudFolder> GetRootFolderAsync(CancellationToken ct)
    {
        var id = await EnsureFolderAsync("root", "Collectary", ct);
        return new CloudFolder(id, "Collectary");
    }

    public async Task<string> EnsureFolderAsync(string parentFolderId, string name, CancellationToken ct)
    {
        var existing = (await ChildrenAsync(parentFolderId, ct))
            .FirstOrDefault(f => f.MimeType == FolderMimeType && f.Name == name);
        if (existing is not null) return existing.Id;

        var request = _drive.Files.Create(new DriveData.File
        {
            Name = name,
            MimeType = FolderMimeType,
            Parents = new[] { parentFolderId },
        });
        request.Fields = "id";
        var created = await request.ExecuteAsync(ct);
        return created.Id;
    }

    public async Task<IReadOnlyList<CloudFile>> ListFilesAsync(string folderId, CancellationToken ct) =>
        (await ChildrenAsync(folderId, ct))
            .Where(f => f.MimeType != FolderMimeType)
            .Select(f => new CloudFile(f.Id, f.Name, f.Size ?? 0))
            .ToList();

    public async Task<IReadOnlyList<CloudFolder>> ListFoldersAsync(string folderId, CancellationToken ct) =>
        (await ChildrenAsync(folderId, ct))
            .Where(f => f.MimeType == FolderMimeType)
            .Select(f => new CloudFolder(f.Id, f.Name))
            .ToList();

    public async Task<byte[]?> DownloadAsync(string folderId, string name, CancellationToken ct)
    {
        var child = (await ChildrenAsync(folderId, ct)).FirstOrDefault(f => f.Name == name);
        if (child is null) return null;

        using var buffer = new MemoryStream();
        var progress = await _drive.Files.Get(child.Id).DownloadAsync(buffer, ct);
        if (progress.Status == Google.Apis.Download.DownloadStatus.Failed)
            throw progress.Exception ?? new IOException($"Download of '{name}' failed.");
        return buffer.ToArray();
    }

    public async Task UploadAsync(string folderId, string name, byte[] content, CancellationToken ct)
    {
        var existing = (await ChildrenAsync(folderId, ct)).FirstOrDefault(f => f.Name == name);
        using var stream = new MemoryStream(content);

        if (existing is null)
        {
            var create = _drive.Files.Create(
                new DriveData.File { Name = name, Parents = new[] { folderId } },
                stream,
                "application/octet-stream");
            create.Fields = "id";
            ThrowIfFailed(await create.UploadAsync(ct), name);
        }
        else
        {
            var update = _drive.Files.Update(new DriveData.File(), existing.Id, stream, "application/octet-stream");
            ThrowIfFailed(await update.UploadAsync(ct), name);
        }
    }

    private void ThrowIfFailed(Google.Apis.Upload.IUploadProgress progress, string name)
    {
        if (progress.Status == Google.Apis.Upload.UploadStatus.Failed)
            throw progress.Exception ?? new IOException($"Upload of '{name}' failed.");
    }

    public async Task DeleteAsync(string folderId, string name, CancellationToken ct)
    {
        var child = (await ChildrenAsync(folderId, ct)).FirstOrDefault(f => f.Name == name);
        if (child is null) return;
        await _drive.Files.Delete(child.Id).ExecuteAsync(ct);
    }

    private async Task<IReadOnlyList<DriveData.File>> ChildrenAsync(string folderId, CancellationToken ct)
    {
        var all = new List<DriveData.File>();
        string? pageToken = null;
        do
        {
            var request = _drive.Files.List();
            request.Q = $"'{ValidId(folderId)}' in parents and trashed = false";
            request.Fields = "nextPageToken,files(id,name,mimeType,size)";
            request.PageSize = 1000;
            request.PageToken = pageToken;
            var response = await request.ExecuteAsync(ct);
            if (response.Files is not null) all.AddRange(response.Files);
            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));
        return all;
    }

    // Drive file ids (and the "root" alias) are limited to letters, digits, '-' and '_'. Rejecting
    // anything else keeps a stray quote from breaking out of the interpolated `Q` query.
    private string ValidId(string folderId)
    {
        if (!string.IsNullOrEmpty(folderId) && folderId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            return folderId;
        throw new ArgumentException($"Unsafe Drive folder id: '{folderId}'", nameof(folderId));
    }
}
