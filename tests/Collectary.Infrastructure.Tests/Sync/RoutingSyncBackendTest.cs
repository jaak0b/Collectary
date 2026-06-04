using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Sync;
using Collectary.Infrastructure.Tests.Infrastructure;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class RoutingSyncBackendTest
{
    private CloudSyncBackend _folder = null!;
    private CloudSyncBackend _oneDrive = null!;
    private CloudProvider _active;

    [SetUp]
    public void SetUp()
    {
        _folder = new CloudSyncBackend(new FakeCloudFileStore());
        _oneDrive = new CloudSyncBackend(new FakeCloudFileStore());
        _active = CloudProvider.Folder;
    }

    private RoutingSyncBackend Build() =>
        new(() => _active, new Dictionary<CloudProvider, Func<ISyncBackend>>
        {
            [CloudProvider.Folder] = () => _folder,
            [CloudProvider.OneDrive] = () => _oneDrive,
        });

    [Test]
    public async Task Write_RoutesToActiveProvider()
    {
        var sut = Build();
        var id = Guid.NewGuid();

        _active = CloudProvider.OneDrive;
        await sut.WriteAsync("items", id, "od", 1);

        Assert.Multiple(() =>
        {
            Assert.That(_oneDrive.ReadAsync("items", id).Result, Is.EqualTo("od"));
            Assert.That(_folder.ReadAsync("items", id).Result, Is.Null);
        });
    }

    [Test]
    public async Task Routing_FollowsActiveProviderChanges()
    {
        var sut = Build();
        var id = Guid.NewGuid();

        _active = CloudProvider.Folder;
        await sut.WriteAsync("items", id, "f", 1);
        _active = CloudProvider.OneDrive;
        await sut.WriteAsync("items", id, "o", 1);

        Assert.Multiple(() =>
        {
            Assert.That(_folder.ReadAsync("items", id).Result, Is.EqualTo("f"));
            Assert.That(_oneDrive.ReadAsync("items", id).Result, Is.EqualTo("o"));
        });
    }

    [Test]
    public void IsAvailable_ReflectsActiveProvider()
    {
        var sut = Build();

        _active = CloudProvider.Folder;
        Assert.That(sut.IsAvailable, Is.True, "fake stores are available");
    }

    [Test]
    public async Task UnregisteredProvider_IsUnavailableAndNoOps()
    {
        var sut = Build();
        _active = CloudProvider.GoogleDrive; // not in the map

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsAvailable, Is.False);
            Assert.That(sut.ListAsync("items").Result, Is.Empty);
            Assert.That(sut.ListBlobKeysAsync("images").Result, Is.Empty);
            Assert.That(sut.ReadAsync("items", Guid.NewGuid()).Result, Is.Null);
            Assert.That(sut.ReadBlobAsync("images", "k").Result, Is.Null);
        });

        // writes/deletes are silently dropped, not thrown
        await sut.WriteAsync("items", Guid.NewGuid(), "x", 1);
        await sut.DeleteAsync("items", Guid.NewGuid());
        await sut.WriteBlobAsync("images", "k", new byte[] { 1 });
        await sut.DeleteBlobAsync("images", "k");
    }

    [Test]
    public void Backend_NotResolved_UntilProviderIsActive()
    {
        var oneDriveResolved = 0;
        var sut = new RoutingSyncBackend(() => _active, new Dictionary<CloudProvider, Func<ISyncBackend>>
        {
            [CloudProvider.Folder] = () => _folder,
            [CloudProvider.OneDrive] = () => { oneDriveResolved++; return _oneDrive; },
        });

        _active = CloudProvider.Folder;
        _ = sut.IsAvailable;
        Assert.That(oneDriveResolved, Is.EqualTo(0), "OneDrive factory must not run while Folder is active");

        _active = CloudProvider.OneDrive;
        _ = sut.IsAvailable;
        _ = sut.IsAvailable;
        Assert.That(oneDriveResolved, Is.EqualTo(1), "factory resolved once then cached");
    }

    [Test]
    public async Task BlobOperations_RouteToActiveProvider()
    {
        var sut = Build();
        _active = CloudProvider.OneDrive;

        await sut.WriteBlobAsync("images", "a.png", new byte[] { 9 });

        Assert.Multiple(() =>
        {
            Assert.That(_oneDrive.ReadBlobAsync("images", "a.png").Result, Is.EqualTo(new byte[] { 9 }));
            Assert.That(sut.ListBlobKeysAsync("images").Result, Is.EquivalentTo(new[] { "a.png" }));
        });
    }
}
