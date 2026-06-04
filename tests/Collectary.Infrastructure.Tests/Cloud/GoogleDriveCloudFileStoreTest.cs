using System.Net;
using System.Text;
using Collectary.Core.Domain;
using Collectary.Infrastructure.Cloud.GoogleDrive;
using Collectary.Infrastructure.Tests.Infrastructure;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace Collectary.Infrastructure.Tests.Cloud;

[TestFixture]
public class GoogleDriveCloudFileStoreTest
{
    private StubHttpMessageHandler _stub = null!;
    private FakeCloudAuthClient _auth = null!;

    [SetUp]
    public void SetUp()
    {
        _stub = new StubHttpMessageHandler();
        _auth = new FakeCloudAuthClient(CloudProvider.GoogleDrive);
    }

    [TearDown]
    public void TearDown() => _stub.Dispose();

    private GoogleDriveCloudFileStore Build(string? rootFolderId = "root")
    {
        var drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientFactory = new StubGoogleHttpClientFactory(_stub),
            ApplicationName = "CollectaryTests",
        });
        return new GoogleDriveCloudFileStore(drive, _auth, () => rootFolderId);
    }

    [Test]
    public void IsAvailable_SignedInWithRoot_True() => Assert.That(Build().IsAvailable, Is.True);

    [Test]
    public void IsAvailable_NoRoot_False() => Assert.That(Build(rootFolderId: null).IsAvailable, Is.False);

    [Test]
    public async Task ListFilesAsync_ReturnsNonFolders()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
            """{"files":[{"id":"f1","name":"a.json","mimeType":"application/octet-stream"},{"id":"d1","name":"sub","mimeType":"application/vnd.google-apps.folder"}]}""");

        var files = await Build().ListFilesAsync("root", CancellationToken.None);

        Assert.That(files.Select(f => f.Name), Is.EquivalentTo(new[] { "a.json" }));
    }

    [Test]
    public async Task ListFoldersAsync_ReturnsFolders()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
            """{"files":[{"id":"f1","name":"a.json","mimeType":"application/octet-stream"},{"id":"d1","name":"sub","mimeType":"application/vnd.google-apps.folder"}]}""");

        var folders = await Build().ListFoldersAsync("root", CancellationToken.None);

        Assert.That(folders.Select(f => (f.Id, f.Name)), Is.EquivalentTo(new[] { ("d1", "sub") }));
    }

    [Test]
    public async Task EnsureFolderAsync_Existing_ReturnsId()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
            """{"files":[{"id":"items-id","name":"items","mimeType":"application/vnd.google-apps.folder"}]}""");

        var id = await Build().EnsureFolderAsync("root", "items", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.EqualTo("items-id"));
            Assert.That(_stub.CountRequests(HttpMethod.Post, "drive/v3/files"), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task EnsureFolderAsync_Missing_CreatesAndReturnsId()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files", """{"files":[]}""")
             .OnJson(HttpMethod.Post, "drive/v3/files", """{"id":"new-items"}""");

        var id = await Build().EnsureFolderAsync("root", "items", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.EqualTo("new-items"));
            Assert.That(_stub.CountRequests(HttpMethod.Post, "drive/v3/files"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DownloadAsync_Existing_ReturnsBytes()
    {
        _stub.OnBytes(HttpMethod.Get, "alt=media", new byte[] { 1, 2, 3 })
             .OnJson(HttpMethod.Get, "drive/v3/files",
                 """{"files":[{"id":"f1","name":"a.json","mimeType":"application/octet-stream"}]}""");

        var bytes = await Build().DownloadAsync("root", "a.json", CancellationToken.None);

        Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public async Task DownloadAsync_Missing_ReturnsNull()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files", """{"files":[]}""");

        var bytes = await Build().DownloadAsync("root", "missing.json", CancellationToken.None);

        Assert.That(bytes, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_Existing_IssuesDelete()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
                 """{"files":[{"id":"f1","name":"a.json","mimeType":"application/octet-stream"}]}""")
             .OnStatus(HttpMethod.Delete, "files/f1", HttpStatusCode.NoContent);

        await Build().DeleteAsync("root", "a.json", CancellationToken.None);

        Assert.That(_stub.CountRequests(HttpMethod.Delete, "files/f1"), Is.EqualTo(1));
    }

    [Test]
    public async Task GetRootFolderAsync_EnsuresCollectaryFolder()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files", """{"files":[]}""")
             .OnJson(HttpMethod.Post, "drive/v3/files", """{"id":"collectary-root"}""");

        var root = await Build(rootFolderId: null).GetRootFolderAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(root.Id, Is.EqualTo("collectary-root"));
            Assert.That(root.Name, Is.EqualTo("Collectary"));
        });
    }

    [Test]
    public async Task UploadAsync_NewFile_UsesResumableUpload()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files", """{"files":[]}""")
             .On(HttpMethod.Post, "upload/drive/v3/files", () =>
             {
                 var response = new HttpResponseMessage(HttpStatusCode.OK)
                 {
                     Content = new StringContent(string.Empty),
                 };
                 response.Headers.Location = new Uri("https://upload.test/session");
                 return response;
             })
             .OnJson(HttpMethod.Put, "upload.test", """{"id":"uploaded","name":"a.json"}""");

        await Build().UploadAsync("root", "a.json", Encoding.UTF8.GetBytes("hi"), CancellationToken.None);

        Assert.That(_stub.CountRequests(HttpMethod.Post, "upload/drive/v3/files"), Is.EqualTo(1));
    }
}
