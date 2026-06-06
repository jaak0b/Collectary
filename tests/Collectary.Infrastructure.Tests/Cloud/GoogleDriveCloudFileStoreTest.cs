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
            GZipEnabled = false, // keep request bodies as plain JSON so tests can assert on them
        });
        return new GoogleDriveCloudFileStore(drive, _auth, () => rootFolderId);
    }

    [Test]
    public void IsAvailable_SignedInWithRoot_True() => Assert.That(Build().IsAvailable, Is.True);

    [Test]
    public void IsAvailable_NoRoot_False() => Assert.That(Build(rootFolderId: null).IsAvailable, Is.False);

    [Test]
    public void RootFolderId_ReturnsConfiguredValue() =>
        Assert.That(Build("my-root").RootFolderId, Is.EqualTo("my-root"));

    [Test]
    public void RootFolderId_NoRoot_ReturnsEmpty() =>
        Assert.That(Build(rootFolderId: null).RootFolderId, Is.Empty);

    [Test]
    public async Task ListFilesAsync_ReturnsNonFolders()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
            """{"files":[{"id":"f1","name":"a.json","mimeType":"application/octet-stream","size":7},{"id":"d1","name":"sub","mimeType":"application/vnd.google-apps.folder"}]}""");

        var files = await Build().ListFilesAsync("root", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(files.Select(f => f.Name), Is.EquivalentTo(new[] { "a.json" }));
            Assert.That(files.Single().Size, Is.EqualTo(7));
            // The query must scope to the parent folder and skip trashed items, projecting mimeType.
            Assert.That(_stub.Requests.Any(r => (r.RequestUri?.ToString() ?? "").Contains("trashed")), Is.True);
            Assert.That(_stub.Requests.Any(r => (r.RequestUri?.ToString() ?? "").Contains("mimeType")), Is.True);
        });
    }

    [Test]
    public async Task ListFilesAsync_FileWithoutSize_DefaultsToZero()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
            """{"files":[{"id":"f1","name":"a.json","mimeType":"application/octet-stream"}]}""");

        var files = await Build().ListFilesAsync("root", CancellationToken.None);

        Assert.That(files.Single().Size, Is.EqualTo(0));
    }

    [Test]
    public async Task ListFilesAsync_NoFilesInResponse_ReturnsEmpty()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files", "{}");

        var files = await Build().ListFilesAsync("root", CancellationToken.None);

        Assert.That(files, Is.Empty);
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
            // Create body must carry the folder name, the folder mime type and the parent.
            Assert.That(_stub.BodyContains(HttpMethod.Post, "drive/v3/files", "items"), Is.True, "name");
            Assert.That(_stub.BodyContains(HttpMethod.Post, "drive/v3/files", "application/vnd.google-apps.folder"), Is.True, "folder mime");
            Assert.That(_stub.BodyContains(HttpMethod.Post, "drive/v3/files", "root"), Is.True, "parent");
            // Asking only for the id keeps the response small.
            Assert.That(_stub.Requests.Any(r => r.Method == HttpMethod.Post && (r.RequestUri?.ToString() ?? "").Contains("fields=id")), Is.True);
        });
    }

    [Test]
    public async Task EnsureFolderAsync_FileWithSameName_IsNotTreatedAsExistingFolder()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
                 """{"files":[{"id":"file-items","name":"items","mimeType":"application/octet-stream"}]}""")
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
    public async Task DeleteAsync_RemovesAllDuplicateNamedCopies()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
                 """{"files":[{"id":"dup1","name":"a.json","mimeType":"application/octet-stream"},{"id":"dup2","name":"a.json","mimeType":"application/octet-stream"}]}""")
             .OnStatus(HttpMethod.Delete, "files/dup1", HttpStatusCode.NoContent)
             .OnStatus(HttpMethod.Delete, "files/dup2", HttpStatusCode.NoContent);

        await Build().DeleteAsync("root", "a.json", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_stub.CountRequests(HttpMethod.Delete, "files/dup1"), Is.EqualTo(1));
            Assert.That(_stub.CountRequests(HttpMethod.Delete, "files/dup2"), Is.EqualTo(1), "every duplicate-named copy must be deleted, not just the first");
        });
    }

    [Test]
    public async Task UploadAsync_WhenDuplicateNamesExist_CollapsesToOne()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
                 """{"files":[{"id":"keep","name":"a.json","mimeType":"application/octet-stream"},{"id":"extra","name":"a.json","mimeType":"application/octet-stream"}]}""")
             .OnStatus(HttpMethod.Delete, "files/extra", HttpStatusCode.NoContent)
             .On(HttpMethod.Patch, "upload/drive/v3/files", () =>
             {
                 var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                 response.Headers.Location = new Uri("https://upload.test/session");
                 return response;
             })
             .OnJson(HttpMethod.Put, "upload.test", """{"id":"keep","name":"a.json"}""");

        await Build().UploadAsync("root", "a.json", Encoding.UTF8.GetBytes("hi"), CancellationToken.None);

        Assert.That(_stub.CountRequests(HttpMethod.Delete, "files/extra"), Is.EqualTo(1),
            "an accidental duplicate name must be collapsed so a single canonical file remains");
    }

    [Test]
    public async Task DeleteAsync_MissingFile_IssuesNoDelete()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files", """{"files":[]}""");

        await Build().DeleteAsync("root", "missing.json", CancellationToken.None);

        Assert.That(_stub.CountRequests(HttpMethod.Delete, "files/"), Is.EqualTo(0));
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
            // Provisioned under the drive's "root", named "Collectary".
            Assert.That(_stub.BodyContains(HttpMethod.Post, "drive/v3/files", "Collectary"), Is.True, "name");
            Assert.That(_stub.BodyContains(HttpMethod.Post, "drive/v3/files", "root"), Is.True, "parent");
        });
    }

    [Test]
    public void ListFilesAsync_WithMalformedFolderId_ThrowsBeforeQuerying()
    {
        // A folder id with a quote would corrupt the interpolated Drive `Q` query; reject it.
        Assert.ThrowsAsync<ArgumentException>(
            () => Build().ListFilesAsync("root' or '1'='1", CancellationToken.None));
        Assert.That(_stub.CountRequests(HttpMethod.Get, "drive/v3/files"), Is.EqualTo(0));
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

        Assert.Multiple(() =>
        {
            Assert.That(_stub.CountRequests(HttpMethod.Post, "upload/drive/v3/files"), Is.EqualTo(1));
            // New-file metadata carries the name and parent folder.
            Assert.That(_stub.BodyContains(HttpMethod.Post, "upload/drive/v3/files", "a.json"), Is.True, "name");
            Assert.That(_stub.BodyContains(HttpMethod.Post, "upload/drive/v3/files", "root"), Is.True, "parent");
            // The resumable session is told the media is octet-stream.
            Assert.That(_stub.Requests.Any(r =>
                r.Headers.TryGetValues("X-Upload-Content-Type", out var v) && v.Any(x => x.Contains("octet-stream"))),
                Is.True, "upload content type");
            // Only the new file's id is requested back, keeping the response minimal.
            Assert.That(_stub.Requests.Any(r => r.Method == HttpMethod.Post
                && (r.RequestUri?.ToString() ?? "").Contains("fields=id")), Is.True, "fields=id");
        });
    }

    [Test]
    public async Task ListFilesAsync_FollowsNextPageToken()
    {
        _stub.OnJson(HttpMethod.Get, "pageToken=tok2",
                 """{"files":[{"id":"f2","name":"b.json","mimeType":"application/octet-stream","size":1}]}""")
             .OnJson(HttpMethod.Get, "drive/v3/files",
                 """{"files":[{"id":"f1","name":"a.json","mimeType":"application/octet-stream","size":1}],"nextPageToken":"tok2"}""");

        var files = await Build().ListFilesAsync("root", CancellationToken.None);

        Assert.That(files.Select(f => f.Name), Is.EquivalentTo(new[] { "a.json", "b.json" }),
            "every page of children must be returned, not just the first");
    }

    [Test]
    public void DownloadAsync_WhenContentRequestFails_Throws()
    {
        _stub.OnStatus(HttpMethod.Get, "alt=media", HttpStatusCode.BadRequest)
             .OnJson(HttpMethod.Get, "drive/v3/files",
                 """{"files":[{"id":"f1","name":"a.json","mimeType":"application/octet-stream"}]}""");

        Assert.That(async () => await Build().DownloadAsync("root", "a.json", CancellationToken.None),
            Throws.Exception, "a failed download must not be reported as success");
    }

    [Test]
    public void UploadAsync_WhenUploadFails_Throws()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files", """{"files":[]}""")
             .On(HttpMethod.Post, "upload/drive/v3/files", () =>
             {
                 var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                 response.Headers.Location = new Uri("https://upload.test/session");
                 return response;
             })
             .OnStatus(HttpMethod.Put, "upload.test", HttpStatusCode.BadRequest);

        Assert.That(async () => await Build().UploadAsync("root", "a.json", Encoding.UTF8.GetBytes("hi"), CancellationToken.None),
            Throws.Exception, "a failed upload must not be reported as success");
    }

    [Test]
    public async Task UploadAsync_ExistingFile_UpdatesInPlace()
    {
        // A file already named "a.json" must be updated (by id), not created a second time.
        _stub.OnJson(HttpMethod.Get, "drive/v3/files",
                 """{"files":[{"id":"existing-id","name":"a.json","mimeType":"application/octet-stream"}]}""")
             .On(HttpMethod.Patch, "upload/drive/v3/files", () =>
             {
                 var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                 response.Headers.Location = new Uri("https://upload.test/session");
                 return response;
             })
             .OnJson(HttpMethod.Put, "upload.test", """{"id":"existing-id","name":"a.json"}""");

        await Build().UploadAsync("root", "a.json", Encoding.UTF8.GetBytes("hi"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Update goes through the existing id and not a create.
            Assert.That(_stub.Requests.Any(r => (r.RequestUri?.ToString() ?? "").Contains("existing-id")), Is.True);
            Assert.That(_stub.CountRequests(HttpMethod.Post, "upload/drive/v3/files"), Is.EqualTo(0));
        });
    }
}
