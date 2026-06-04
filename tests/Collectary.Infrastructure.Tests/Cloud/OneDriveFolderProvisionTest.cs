using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Infrastructure.Cloud.OneDrive;
using Collectary.Infrastructure.Tests.Infrastructure;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Collectary.Infrastructure.Tests.Cloud;

[TestFixture]
public class OneDriveFolderProvisionTest
{
    private const string DriveJson = """{"id":"drive1"}""";

    private StubHttpMessageHandler _stub = null!;

    [SetUp]
    public void SetUp() => _stub = new StubHttpMessageHandler();

    [TearDown]
    public void TearDown() => _stub.Dispose();

    private OneDriveCloudFileStore Build()
    {
        var auth = new FakeCloudAuthClient();
        var authProvider = new BaseBearerTokenAuthenticationProvider(new GraphAccessTokenProvider(auth));
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: new HttpClient(_stub, disposeHandler: false));
        var graph = new GraphServiceClient(adapter);
        return new OneDriveCloudFileStore(graph, auth, () => null);
    }

    [Test]
    public async Task GetRootFolderAsync_ReturnsDriveRoot()
    {
        _stub.OnJson(HttpMethod.Get, "me/drive", DriveJson)
             .OnJson(HttpMethod.Get, "items/root", """{"id":"root-id","name":"OneDrive"}""");

        var root = await Build().GetRootFolderAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(root.Id, Is.EqualTo("root-id"));
            Assert.That(root.Name, Is.EqualTo("OneDrive"));
        });
    }
}
