using System.Net;
using System.Text;
using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Infrastructure.Cloud.OneDrive;
using Collectary.Infrastructure.Tests.Infrastructure;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Collectary.Infrastructure.Tests.Cloud;

[TestFixture]
public class OneDriveCloudFileStoreTest
{
    private const string DriveJson = """{"id":"drive1"}""";

    private StubHttpMessageHandler _stub = null!;
    private FakeCloudAuthClient _auth = null!;

    [SetUp]
    public void SetUp()
    {
        _stub = new StubHttpMessageHandler();
        _auth = new FakeCloudAuthClient();
    }

    [TearDown]
    public void TearDown() => _stub.Dispose();

    private OneDriveCloudFileStore Build(string? rootFolderId = "root", int largeUploadThreshold = 4 * 1024 * 1024)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(new GraphAccessTokenProvider(_auth));
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: new HttpClient(_stub, disposeHandler: false));
        var graph = new GraphServiceClient(adapter);
        return new OneDriveCloudFileStore(graph, _auth, () => rootFolderId, largeUploadThreshold);
    }

    [Test]
    public void IsAvailable_SignedInWithRoot_True() =>
        Assert.That(Build().IsAvailable, Is.True);

    [Test]
    public void IsAvailable_NoRoot_False() =>
        Assert.That(Build(rootFolderId: null).IsAvailable, Is.False);

    [Test]
    public void IsAvailable_NotSignedIn_False()
    {
        _auth.IsSignedIn = false;
        Assert.That(Build().IsAvailable, Is.False);
    }

    [Test]
    public void RootFolderId_ReturnsConfiguredValue() =>
        Assert.That(Build("my-root").RootFolderId, Is.EqualTo("my-root"));

    [Test]
    public async Task ListFilesAsync_ReturnsFilesNotFolders()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children",
                 """{"value":[{"id":"f1","name":"a.json","size":3},{"id":"d1","name":"sub","folder":{"childCount":0}}]}""");

        var files = await Build().ListFilesAsync("root", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(files.Select(f => f.Name), Is.EquivalentTo(new[] { "a.json" }));
            Assert.That(files.Single().Id, Is.EqualTo("f1"));
            Assert.That(files.Single().Size, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task ListFoldersAsync_ReturnsFoldersNotFiles()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children",
                 """{"value":[{"id":"f1","name":"a.json","size":3},{"id":"d1","name":"sub","folder":{"childCount":0}}]}""");

        var folders = await Build().ListFoldersAsync("root", CancellationToken.None);

        Assert.That(folders.Select(f => (f.Id, f.Name)), Is.EquivalentTo(new[] { ("d1", "sub") }));
    }

    [Test]
    public async Task EnsureFolderAsync_ExistingFolder_ReturnsItsId()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children",
                 """{"value":[{"id":"items-id","name":"items","folder":{"childCount":0}}]}""");

        var id = await Build().EnsureFolderAsync("root", "items", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.EqualTo("items-id"));
            Assert.That(_stub.CountRequests(HttpMethod.Post, "/children"), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task EnsureFolderAsync_MissingFolder_CreatesAndReturnsId()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", """{"value":[]}""")
             .OnJson(HttpMethod.Post, "/children", """{"id":"new-items","name":"items","folder":{"childCount":0}}""");

        var id = await Build().EnsureFolderAsync("root", "items", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.EqualTo("new-items"));
            Assert.That(_stub.CountRequests(HttpMethod.Post, "/children"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DownloadAsync_ExistingFile_ReturnsBytes()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", """{"value":[{"id":"f1","name":"a.json","size":3}]}""")
             .OnBytes(HttpMethod.Get, "/content", new byte[] { 1, 2, 3 });

        var bytes = await Build().DownloadAsync("root", "a.json", CancellationToken.None);

        Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public async Task DownloadAsync_MissingName_ReturnsNull()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", """{"value":[]}""");

        var bytes = await Build().DownloadAsync("root", "missing.json", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.Null);
            Assert.That(_stub.CountRequests(HttpMethod.Get, "/content"), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task UploadAsync_PutsContentToNamedPath()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Put, "/content", """{"id":"uploaded","name":"a.json"}""");

        await Build().UploadAsync("root", "a.json", Encoding.UTF8.GetBytes("hi"), CancellationToken.None);

        Assert.That(_stub.CountRequests(HttpMethod.Put, "/content"), Is.EqualTo(1));
    }

    [Test]
    public async Task UploadAsync_LargeFile_UsesUploadSession()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Post, "createUploadSession",
                 """{"uploadUrl":"https://upload.test/sess","expirationDateTime":"2099-01-01T00:00:00Z","nextExpectedRanges":["0-"]}""")
             .OnJson(HttpMethod.Put, "upload.test", """{"id":"big","name":"a.json"}""", HttpStatusCode.Created);

        await Build(largeUploadThreshold: 1).UploadAsync("root", "a.json", new byte[] { 1, 2, 3, 4 }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_stub.CountRequests(HttpMethod.Post, "createUploadSession"), Is.EqualTo(1));
            Assert.That(_stub.CountRequests(HttpMethod.Put, "/content"), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task UploadAsync_SmallFile_UsesSimplePut()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Put, "/content", """{"id":"uploaded","name":"a.json"}""");

        await Build().UploadAsync("root", "a.json", new byte[] { 1, 2, 3, 4 }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_stub.CountRequests(HttpMethod.Put, "/content"), Is.EqualTo(1));
            Assert.That(_stub.CountRequests(HttpMethod.Post, "createUploadSession"), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task DeleteAsync_ExistingFile_IssuesDelete()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", """{"value":[{"id":"f1","name":"a.json","size":3}]}""")
             .OnStatus(HttpMethod.Delete, "/items/f1", HttpStatusCode.NoContent);

        await Build().DeleteAsync("root", "a.json", CancellationToken.None);

        Assert.That(_stub.CountRequests(HttpMethod.Delete, "/items/f1"), Is.EqualTo(1));
    }

    [Test]
    public async Task DeleteAsync_MissingFile_IssuesNoDelete()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", """{"value":[]}""");

        await Build().DeleteAsync("root", "missing.json", CancellationToken.None);

        Assert.That(_stub.CountRequests(HttpMethod.Delete, "/items/"), Is.EqualTo(0));
    }

    [Test]
    public async Task DriveId_ResolvedOnce_AndCachedAcrossCalls()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", """{"value":[]}""");
        var store = Build();

        await store.ListFilesAsync("root", CancellationToken.None);
        await store.ListFoldersAsync("root", CancellationToken.None);

        Assert.That(_stub.CountRequests(HttpMethod.Get, "me/drive"), Is.EqualTo(1));
    }

    [Test]
    public void DriveId_WhenDriveHasNoId_Throws()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", "{}")
             .OnJson(HttpMethod.Get, "/children", """{"value":[]}""");

        Assert.That(async () => await Build().ListFilesAsync("root", CancellationToken.None),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public async Task DownloadAsync_EmptyContentResponse_ReturnsNull()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", """{"value":[{"id":"f1","name":"a.json","size":0}]}""")
             .OnStatus(HttpMethod.Get, "/content", HttpStatusCode.NoContent);

        var bytes = await Build().DownloadAsync("root", "a.json", CancellationToken.None);

        Assert.That(bytes, Is.Null);
    }
}
