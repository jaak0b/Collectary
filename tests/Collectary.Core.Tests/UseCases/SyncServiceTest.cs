using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class SyncServiceTest
{
    private ISyncBackend _backend = null!;
    private ISyncStore _store = null!;
    private ISyncSerializer _serializer = null!;
    private IDeviceIdentity _device = null!;

    [SetUp]
    public void SetUp()
    {
        _backend = A.Fake<ISyncBackend>();
        _store = A.Fake<ISyncStore>();
        _serializer = A.Fake<ISyncSerializer>();
        _device = A.Fake<IDeviceIdentity>();

        A.CallTo(() => _device.DeviceId).Returns(Guid.NewGuid());
        A.CallTo(() => _backend.IsAvailable).Returns(true);
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind)).Returns(new List<Guid>());
        A.CallTo(() => _store.GetAllUsersAsync()).Returns(new List<User>());
        A.CallTo(() => _store.GetAllSharedFieldsAsync()).Returns(new List<SharedField>());
        A.CallTo(() => _store.GetAllPresetsAsync()).Returns(new List<Preset>());
        A.CallTo(() => _store.GetAllItemsAsync()).Returns(new List<Item>());
        A.CallTo(() => _store.GetAllSharesAsync()).Returns(new List<CollectionShare>());
        A.CallTo(() => _store.GetTombstoneIdsAsync()).Returns(new List<Guid>());
        A.CallTo(() => _store.GetMaxObservedLamportAsync()).Returns(0L);
    }

    [Test]
    public async Task SyncAsync_WhenAnEntityFailsToApply_CountsItAsSkippedWithoutAborting()
    {
        var remoteDevice = Guid.NewGuid();
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { remoteDevice });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, remoteDevice))
            .Returns(new SnapshotIntegrity().Wrap("payload"));

        var item = new Item { Id = Guid.NewGuid(), DisplayName = "Boom", PresetId = Guid.NewGuid(), Lamport = 5 };
        var snapshot = new DeviceSnapshot { DeviceId = remoteDevice, Items = { item } };
        A.CallTo(() => _serializer.Deserialize<DeviceSnapshot>("payload")).Returns(snapshot);
        A.CallTo(() => _store.ApplyItemAsync(A<Item>._)).Throws(new InvalidOperationException("cannot apply"));

        var sut = new SyncService(_backend, _store, _serializer, _device);

        var result = await sut.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Skipped, Is.EqualTo(1), "the entity that failed to apply is reported as skipped");
            Assert.That(result.Pulled, Is.EqualTo(0), "a skipped entity is not counted as pulled");
        });
    }

    [Test]
    public async Task SyncAsync_WhenBackendUnavailable_ReportsBackendUnavailableInsteadOfLookingLikeSuccess()
    {
        A.CallTo(() => _backend.IsAvailable).Returns(false);
        var sut = new SyncService(_backend, _store, _serializer, _device);

        var result = await sut.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.BackendUnavailable, Is.True, "an unavailable backend must be distinguishable from a clean sync");
            Assert.That(result.Pushed, Is.EqualTo(0));
            Assert.That(result.Pulled, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task SyncAsync_WhenARemoteSnapshotFailsItsChecksum_CountsItAsUnreadableAndKeepsGoing()
    {
        var corruptDevice = Guid.NewGuid();
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { corruptDevice });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, corruptDevice))
            .Returns("sha256:0000000000000000000000000000000000000000000000000000000000000000\n{\"DeviceId\":\"x\"}");

        var sut = new SyncService(_backend, _store, _serializer, _device);

        var result = await sut.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.UnreadableDevices, Is.EqualTo(1), "a corrupt peer snapshot is reported, not silently dropped");
            Assert.That(result.Skipped, Is.EqualTo(0));
        });
        A.CallTo(() => _serializer.Deserialize<DeviceSnapshot>(A<string>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task SyncAsync_WhenARemoteSnapshotFailsToDeserialize_CountsItAsUnreadable()
    {
        var badDevice = Guid.NewGuid();
        var content = new SnapshotIntegrity().Wrap("{\"DeviceId\":\"x\"}");
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { badDevice });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, badDevice)).Returns(content);
        A.CallTo(() => _serializer.Deserialize<DeviceSnapshot>(A<string>._))
            .Throws(new InvalidOperationException("malformed snapshot"));

        var sut = new SyncService(_backend, _store, _serializer, _device);

        var result = await sut.SyncAsync();

        Assert.That(result.UnreadableDevices, Is.EqualTo(1),
            "a peer whose verified body still fails to deserialize is reported, not silently dropped");
    }

    [Test]
    public async Task SyncAsync_WhenARemoteSnapshotHasNoChecksumHeader_CountsItAsUnreadable()
    {
        var foreignDevice = Guid.NewGuid();
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { foreignDevice });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, foreignDevice)).Returns("{\"DeviceId\":\"x\"}");

        var sut = new SyncService(_backend, _store, _serializer, _device);

        var result = await sut.SyncAsync();

        Assert.That(result.UnreadableDevices, Is.EqualTo(1), "an unverified (unprefixed) peer file is skipped and reported");
    }

    [Test]
    public async Task SyncAsync_WhenOneImageFailsToTransfer_CountsItAndStillTransfersTheOthers()
    {
        var images = A.Fake<IImageStore>();
        A.CallTo(() => images.ListKeysAsync()).Returns(new List<string> { "bad", "good" });
        A.CallTo(() => _backend.ListBlobKeysAsync(SyncService.ImageKind)).Returns(new List<string>());
        A.CallTo(() => _store.GetReferencedImageKeysAsync()).Returns(new List<string> { "bad", "good" });
        A.CallTo(() => images.Open(A<string>._)).ReturnsLazily(() => new MemoryStream(new byte[] { 1, 2, 3 }));
        A.CallTo(() => _backend.WriteBlobAsync(SyncService.ImageKind, "bad", A<byte[]>._))
            .Throws(new InvalidOperationException("upload failed"));

        var sut = new SyncService(_backend, _store, _serializer, _device, images);

        var result = await sut.SyncAsync();

        Assert.That(result.ImagesFailed, Is.EqualTo(1), "a single failed image is isolated and reported, not allowed to abort the run");
        A.CallTo(() => _backend.WriteBlobAsync(SyncService.ImageKind, "good", A<byte[]>._))
            .MustHaveHappenedOnceExactly();
    }

    private void StubPeer(Guid peerId, string content)
    {
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { _device.DeviceId, peerId });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, peerId)).Returns(content);
        A.CallTo(() => _serializer.Deserialize<DeviceSnapshot>(A<string>._))
            .Returns(new DeviceSnapshot { DeviceId = peerId });
    }

    private void CaptureFingerprint()
    {
        string? persisted = null;
        A.CallTo(() => _store.SetSyncFingerprintAsync(A<string>._))
            .Invokes((string f) => persisted = f).Returns(Task.CompletedTask);
        A.CallTo(() => _store.GetSyncFingerprintAsync()).ReturnsLazily(() => persisted);
    }

    [Test]
    public async Task SyncAsync_SecondRunWithNothingChanged_SkipsTheHeavyHalf()
    {
        var peerId = Guid.NewGuid();
        StubPeer(peerId, new SnapshotIntegrity().Wrap("{}"));
        CaptureFingerprint();
        var sut = new SyncService(_backend, _store, _serializer, _device);

        await sut.SyncAsync();
        await sut.SyncAsync();

        Assert.Multiple(() =>
        {
            A.CallTo(() => _store.GetAllItemsAsync()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _backend.WriteAsync(SyncService.DevicesKind, _device.DeviceId, A<string>._))
                .MustHaveHappenedOnceExactly();
        });
    }

    [Test]
    public async Task SyncAsync_WhenAPeerFileChanged_RunsTheFullPathAgain()
    {
        var peerId = Guid.NewGuid();
        StubPeer(peerId, new SnapshotIntegrity().Wrap("{}"));
        CaptureFingerprint();
        var sut = new SyncService(_backend, _store, _serializer, _device);
        await sut.SyncAsync();

        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, peerId))
            .Returns(new SnapshotIntegrity().Wrap("{\"changed\":1}"));
        await sut.SyncAsync();

        A.CallTo(() => _backend.WriteAsync(SyncService.DevicesKind, _device.DeviceId, A<string>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Test]
    public async Task SyncAsync_WhenDirtyEvenThoughPeersUnchanged_RunsTheFullPath()
    {
        var peerId = Guid.NewGuid();
        StubPeer(peerId, new SnapshotIntegrity().Wrap("{}"));
        CaptureFingerprint();
        var sut = new SyncService(_backend, _store, _serializer, _device);
        await sut.SyncAsync();

        A.CallTo(() => _store.HasDirtyEntitiesAsync()).Returns(true);
        await sut.SyncAsync();

        A.CallTo(() => _backend.WriteAsync(SyncService.DevicesKind, _device.DeviceId, A<string>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Test]
    public async Task SyncAsync_WhenOwnFileIsMissingFromTheListing_RunsTheFullPathToSelfHeal()
    {
        var peerId = Guid.NewGuid();
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { peerId });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, peerId)).Returns(new SnapshotIntegrity().Wrap("{}"));
        A.CallTo(() => _serializer.Deserialize<DeviceSnapshot>(A<string>._)).Returns(new DeviceSnapshot { DeviceId = peerId });
        CaptureFingerprint();
        var sut = new SyncService(_backend, _store, _serializer, _device);

        await sut.SyncAsync();
        await sut.SyncAsync();

        A.CallTo(() => _backend.WriteAsync(SyncService.DevicesKind, _device.DeviceId, A<string>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Test]
    public async Task SyncAsync_WhenAReferencedImageExistsLocallyAndRemotely_NeitherUploadsNorRedownloadsIt()
    {
        var images = A.Fake<IImageStore>();
        A.CallTo(() => images.ListKeysAsync()).Returns(new List<string> { "both" });
        A.CallTo(() => images.Open(A<string>._)).ReturnsLazily(() => new MemoryStream(new byte[] { 1 }));
        A.CallTo(() => _backend.ListBlobKeysAsync(SyncService.ImageKind)).Returns(new List<string> { "both" });
        A.CallTo(() => _store.GetReferencedImageKeysAsync()).Returns(new List<string> { "both" });

        var sut = new SyncService(_backend, _store, _serializer, _device, images);

        await sut.SyncAsync();

        Assert.Multiple(() =>
        {
            A.CallTo(() => _backend.ReadBlobAsync(SyncService.ImageKind, "both")).MustNotHaveHappened();
            A.CallTo(() => _backend.WriteBlobAsync(SyncService.ImageKind, "both", A<byte[]>._)).MustNotHaveHappened();
        });
    }

    [Test]
    public async Task SyncAsync_AfterPullingARemoteEntity_PersistsItsLamportAsTheNewHighWaterMark()
    {
        var remoteDevice = Guid.NewGuid();
        StubPeer(remoteDevice, new SnapshotIntegrity().Wrap("payload"));
        var item = new Item { Id = Guid.NewGuid(), DisplayName = "Hot", PresetId = Guid.NewGuid(), Lamport = 41 };
        A.CallTo(() => _serializer.Deserialize<DeviceSnapshot>(A<string>._))
            .Returns(new DeviceSnapshot { DeviceId = remoteDevice, Items = { item } });
        var sut = new SyncService(_backend, _store, _serializer, _device);

        await sut.SyncAsync();

        A.CallTo(() => _store.SetMaxObservedLamportAsync(41L)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SyncAsync_WhenAListedPeerFileCannotBeRead_ReportsItUnreadable()
    {
        var peerId = Guid.NewGuid();
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { _device.DeviceId, peerId });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, peerId)).Returns(Task.FromResult<string?>(null));
        var sut = new SyncService(_backend, _store, _serializer, _device);

        var result = await sut.SyncAsync();

        Assert.That(result.UnreadableDevices, Is.EqualTo(1),
            "a peer that is listed but cannot be read is reported, not silently dropped");
    }

    [Test]
    public async Task SyncAsync_WhenAListedPeerFileCannotBeRead_DoesNotPersistTheFingerprint()
    {
        var peerId = Guid.NewGuid();
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { _device.DeviceId, peerId });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, peerId)).Returns(Task.FromResult<string?>(null));
        var sut = new SyncService(_backend, _store, _serializer, _device);

        await sut.SyncAsync();

        A.CallTo(() => _store.SetSyncFingerprintAsync(A<string>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task SyncAsync_WhenANewPeerAppearsButCannotBeRead_DoesNotIdleSkip()
    {
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { _device.DeviceId });
        CaptureFingerprint();
        var sut = new SyncService(_backend, _store, _serializer, _device);
        await sut.SyncAsync();

        var peerId = Guid.NewGuid();
        A.CallTo(() => _backend.ListAsync(SyncService.DevicesKind))
            .Returns(new List<Guid> { _device.DeviceId, peerId });
        A.CallTo(() => _backend.ReadAsync(SyncService.DevicesKind, peerId)).Returns(Task.FromResult<string?>(null));
        var result = await sut.SyncAsync();

        Assert.That(result.UnreadableDevices, Is.EqualTo(1),
            "an unreadable new peer must not be masked by the idle fast-path");
        A.CallTo(() => _backend.WriteAsync(SyncService.DevicesKind, _device.DeviceId, A<string>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Test]
    public async Task SyncAsync_WhenNoBlobsExistAnywhere_SkipsTheExpensiveReferencedImageScan()
    {
        var images = A.Fake<IImageStore>();
        A.CallTo(() => images.ListKeysAsync()).Returns(new List<string>());
        A.CallTo(() => _backend.ListBlobKeysAsync(SyncService.ImageKind)).Returns(new List<string>());
        A.CallTo(() => _store.GetReferencedImageKeysAsync())
            .Throws(new InvalidOperationException("must not scan items for image keys when no blobs exist anywhere"));

        var sut = new SyncService(_backend, _store, _serializer, _device, images);

        await sut.SyncAsync();

        A.CallTo(() => _store.GetReferencedImageKeysAsync()).MustNotHaveHappened();
    }
}
