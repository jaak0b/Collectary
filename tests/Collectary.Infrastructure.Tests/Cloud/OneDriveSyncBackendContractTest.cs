using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Infrastructure.Cloud.OneDrive;
using Collectary.Infrastructure.Sync;
using Collectary.Infrastructure.Tests.Infrastructure;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Collectary.Infrastructure.Tests.Cloud;

/// <summary>
/// End-to-end: <see cref="CloudSyncBackend"/> over a real <see cref="OneDriveCloudFileStore"/>
/// (Graph SDK) backed by a stubbed transport. Proves the layers compose and the
/// {id}.{rev}.json document layout survives a round-trip through Graph.
/// </summary>
[TestFixture]
public class OneDriveSyncBackendContractTest
{
    private const string DriveJson = """{"id":"drive1"}""";

    private StubHttpMessageHandler _stub = null!;

    [SetUp]
    public void SetUp() => _stub = new StubHttpMessageHandler();

    [TearDown]
    public void TearDown() => _stub.Dispose();

    private CloudSyncBackend Build()
    {
        var auth = new FakeCloudAuthClient();
        var authProvider = new BaseBearerTokenAuthenticationProvider(new GraphAccessTokenProvider(auth));
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: new HttpClient(_stub, disposeHandler: false));
        var graph = new GraphServiceClient(adapter);
        return new CloudSyncBackend(new OneDriveCloudFileStore(graph, auth, () => "root"));
    }

    [Test]
    public async Task WriteAsync_CreatesKindFolderAndUploadsDocument()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", """{"value":[]}""")
             .OnJson(HttpMethod.Post, "/children", """{"id":"items-folder","name":"items","folder":{}}""")
             .OnJson(HttpMethod.Put, "/content", """{"id":"doc","name":"doc.json"}""");

        await Build().WriteAsync("items", Guid.NewGuid(), "{\"x\":1}", 1);

        Assert.Multiple(() =>
        {
            Assert.That(_stub.CountRequests(HttpMethod.Post, "/children"), Is.EqualTo(1), "should create the 'items' folder");
            Assert.That(_stub.CountRequests(HttpMethod.Put, "/content"), Is.EqualTo(1), "should upload the document");
        });
    }

    [Test]
    public async Task ListAsync_ParsesDocumentNamesFromGraphChildren()
    {
        var id = Guid.NewGuid();
        var childrenJson =
            "{\"value\":[{\"id\":\"items-folder\",\"name\":\"items\",\"folder\":{}},"
            + "{\"id\":\"doc1\",\"name\":\"" + $"{id:N}.7.json" + "\",\"size\":5}]}";
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "/children", childrenJson);

        var entries = await Build().ListAsync("items");

        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(e => e.Id), Is.EquivalentTo(new[] { id }));
            Assert.That(entries.Single().Revision, Is.EqualTo(7));
        });
    }
}
