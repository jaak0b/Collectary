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
        await _drive.Files.Get(child.Id).DownloadAsync(buffer, ct);
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
            await create.UploadAsync(ct);
        }
        else
        {
            var update = _drive.Files.Update(new DriveData.File(), existing.Id, stream, "application/octet-stream");
            await update.UploadAsync(ct);
        }
    }

    public async Task DeleteAsync(string folderId, string name, CancellationToken ct)
    {
        var child = (await ChildrenAsync(folderId, ct)).FirstOrDefault(f => f.Name == name);
        if (child is null) return;
        await _drive.Files.Delete(child.Id).ExecuteAsync(ct);
    }

    private async Task<IReadOnlyList<DriveData.File>> ChildrenAsync(string folderId, CancellationToken ct)
    {
        var request = _drive.Files.List();
        request.Q = $"'{folderId}' in parents and trashed = false";
        request.Fields = "files(id,name,mimeType,size)";
        request.PageSize = 1000;
        var response = await request.ExecuteAsync(ct);
        return (IReadOnlyList<DriveData.File>?)response.Files ?? Array.Empty<DriveData.File>();
    }
}
