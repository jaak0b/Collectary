using System.Net;
using Collectary.Core.Domain;
using Collectary.Infrastructure.Cloud.GoogleDrive;
using Collectary.Infrastructure.Sync;
using Collectary.Infrastructure.Tests.Infrastructure;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace Collectary.Infrastructure.Tests.Cloud;

/// <summary>
/// End-to-end: <see cref="CloudSyncBackend"/> over a real <see cref="GoogleDriveCloudFileStore"/>
/// (Drive SDK) backed by a stubbed transport. Proves the layers compose and a document write reaches
/// Drive via the resumable upload path.
/// </summary>
[TestFixture]
public class GoogleDriveSyncBackendContractTest
{
    private StubHttpMessageHandler _stub = null!;

    [SetUp]
    public void SetUp() => _stub = new StubHttpMessageHandler();

    [TearDown]
    public void TearDown() => _stub.Dispose();

    private CloudSyncBackend Build()
    {
        var auth = new FakeCloudAuthClient(CloudProvider.GoogleDrive);
        var drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientFactory = new StubGoogleHttpClientFactory(_stub),
            ApplicationName = "CollectaryTests",
        });
        return new CloudSyncBackend(new GoogleDriveCloudFileStore(drive, auth, () => "root"));
    }

    [Test]
    public async Task WriteAsync_CreatesKindFolderAndUploadsDocument()
    {
        _stub.OnJson(HttpMethod.Get, "drive/v3/files", """{"files":[]}""")
             .OnJson(HttpMethod.Post, "drive/v3/files", """{"id":"items-folder"}""")
             .On(HttpMethod.Post, "upload/drive/v3/files", () =>
             {
                 var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                 response.Headers.Location = new Uri("https://upload.test/session");
                 return response;
             })
             .OnJson(HttpMethod.Put, "upload.test", """{"id":"doc","name":"doc.json"}""");

        await Build().WriteAsync("items", Guid.NewGuid(), "{\"x\":1}", 1);

        Assert.Multiple(() =>
        {
            Assert.That(_stub.CountRequests(HttpMethod.Post, "drive/v3/files"), Is.GreaterThanOrEqualTo(1), "creates the 'items' folder");
            Assert.That(_stub.CountRequests(HttpMethod.Post, "upload/drive/v3/files"), Is.EqualTo(1), "uploads the document");
        });
    }
}
