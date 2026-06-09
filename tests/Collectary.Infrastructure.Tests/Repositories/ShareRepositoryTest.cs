using Collectary.Core.Domain;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Repositories;

[TestFixture]
public class ShareRepositoryTest : DbIntegrationTestBase
{
    private ShareRepository _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new ShareRepository(DbFactory);
    }

    private static CollectionShare Make(Guid presetId, Guid userId, SharePermission permission = SharePermission.Read) =>
        new() { PresetId = presetId, SharedWithUserId = userId, GrantedByUserId = Guid.NewGuid(), Permission = permission };

    [Test]
    public async Task AddOrUpdateAsync_ThenGet_ReturnsShare()
    {
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Edit));

        var loaded = await _sut.GetAsync(presetId, userId);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Permission, Is.EqualTo(SharePermission.Edit));
    }

    [Test]
    public async Task AddOrUpdateAsync_WhenExisting_UpdatesPermission()
    {
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Read));
        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Edit));

        var loaded = await _sut.GetAsync(presetId, userId);

        Assert.That(loaded!.Permission, Is.EqualTo(SharePermission.Edit));
        Assert.That(await _sut.GetByPresetAsync(presetId), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RemoveAsync_RemovesShare()
    {
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, userId));

        await _sut.RemoveAsync(presetId, userId);

        Assert.That(await _sut.GetAsync(presetId, userId), Is.Null);
    }

    [Test]
    public async Task GetForUserAsync_ReturnsOnlyThatUsersShares()
    {
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(Guid.NewGuid(), userId));
        await _sut.AddOrUpdateAsync(Make(Guid.NewGuid(), userId));
        await _sut.AddOrUpdateAsync(Make(Guid.NewGuid(), Guid.NewGuid()));

        var result = await _sut.GetForUserAsync(userId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task RemoveAllForPresetAsync_RemovesEveryShare()
    {
        var presetId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, Guid.NewGuid()));
        await _sut.AddOrUpdateAsync(Make(presetId, Guid.NewGuid()));

        await _sut.RemoveAllForPresetAsync(presetId);

        Assert.That(await _sut.GetByPresetAsync(presetId), Is.Empty);
    }

    [Test]
    public async Task GetAsync_WhenMissing_ReturnsNull() =>
        Assert.That(await _sut.GetAsync(Guid.NewGuid(), Guid.NewGuid()), Is.Null);

    [Test]
    public async Task AddOrUpdateAsync_StampsTheShareDirtySoItSyncs()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var share = Make(Guid.NewGuid(), Guid.NewGuid(), SharePermission.Edit);

        await _sut.AddOrUpdateAsync(share);

        var stored = (await store.GetAllSharesAsync()).Single(s => s.Id == share.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.IsDirty, Is.True, "a new share must be dirty so the next sync pushes it to the folder");
            Assert.That(stored.Revision, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task AddOrUpdateAsync_WhenUpdatingExisting_BumpsRevisionAndStaysDirty()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Read));
        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Edit));

        var stored = (await store.GetAllSharesAsync()).Single(s => s.PresetId == presetId && s.SharedWithUserId == userId);
        Assert.Multiple(() =>
        {
            Assert.That(stored.IsDirty, Is.True);
            Assert.That(stored.Revision, Is.EqualTo(2), "a permission change must bump the revision so it re-syncs");
        });
    }

    [Test]
    public async Task RemoveAsync_HardDeletesAndRecordsTombstone()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, userId));
        var id = (await store.GetAllSharesAsync()).Single(s => s.PresetId == presetId && s.SharedWithUserId == userId).Id;

        await _sut.RemoveAsync(presetId, userId);

        var rows = (await store.GetAllSharesAsync()).Where(s => s.PresetId == presetId && s.SharedWithUserId == userId).ToList();
        var tombstones = await store.GetTombstoneIdsAsync();
        Assert.Multiple(() =>
        {
            Assert.That(rows, Is.Empty, "a revoked share is hard-deleted");
            Assert.That(tombstones, Does.Contain(id), "a tombstone records the revocation so it syncs");
        });
    }

    [Test]
    public async Task AddOrUpdateAsync_AfterRevoke_RegrantsAccessWithoutCollision()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Read));
        await _sut.RemoveAsync(presetId, userId);

        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Edit));

        var rows = (await store.GetAllSharesAsync()).Where(s => s.PresetId == presetId && s.SharedWithUserId == userId).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(1), "re-granting leaves exactly one live row, not a duplicate that violates the unique index");
            Assert.That(rows[0].Permission, Is.EqualTo(SharePermission.Edit));
        });
        Assert.That(await _sut.GetAsync(presetId, userId), Is.Not.Null, "the re-granted share grants access again");
    }

    [Test]
    public async Task RemoveAllForPresetAsync_HardDeletesEveryShareAndRecordsTombstones()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var presetId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, Guid.NewGuid()));
        await _sut.AddOrUpdateAsync(Make(presetId, Guid.NewGuid()));
        var ids = (await store.GetAllSharesAsync()).Where(s => s.PresetId == presetId).Select(s => s.Id).ToList();

        await _sut.RemoveAllForPresetAsync(presetId);

        var rows = (await store.GetAllSharesAsync()).Where(s => s.PresetId == presetId).ToList();
        var tombstones = await store.GetTombstoneIdsAsync();
        var stillGranted = await _sut.GetByPresetAsync(presetId);
        Assert.Multiple(() =>
        {
            Assert.That(stillGranted, Is.Empty, "revoked shares no longer grant access");
            Assert.That(rows, Is.Empty, "every share row is hard-deleted");
            Assert.That(ids.All(tombstones.Contains), Is.True, "each revocation is recorded as a tombstone so it syncs");
        });
    }
}
