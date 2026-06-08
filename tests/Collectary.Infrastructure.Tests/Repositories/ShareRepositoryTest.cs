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
    public async Task RemoveAsync_SoftDeletesAndLeavesADirtyTombstoneSoTheRevocationSyncs()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, userId));

        await _sut.RemoveAsync(presetId, userId);

        var tombstone = (await store.GetAllSharesAsync()).Single(s => s.PresetId == presetId && s.SharedWithUserId == userId);
        Assert.Multiple(() =>
        {
            Assert.That(tombstone.IsDeleted, Is.True, "a revoked share must become a tombstone, not a hard delete, so the revocation propagates");
            Assert.That(tombstone.IsDirty, Is.True, "the tombstone must be dirty so the next sync pushes the revocation");
            Assert.That(tombstone.DeletedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task AddOrUpdateAsync_AfterRevoke_RevivesTheSameRowWithoutDuplicateOrCollision()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Read));
        var originalId = (await store.GetAllSharesAsync()).Single(s => s.PresetId == presetId && s.SharedWithUserId == userId).Id;
        await _sut.RemoveAsync(presetId, userId);

        await _sut.AddOrUpdateAsync(Make(presetId, userId, SharePermission.Edit));

        var rows = (await store.GetAllSharesAsync()).Where(s => s.PresetId == presetId && s.SharedWithUserId == userId).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(1), "re-granting must revive the tombstone, not insert a second row that violates the unique index");
            Assert.That(rows[0].Id, Is.EqualTo(originalId), "the revived row keeps the original id");
            Assert.That(rows[0].IsDeleted, Is.False, "re-granting clears the tombstone");
            Assert.That(rows[0].Permission, Is.EqualTo(SharePermission.Edit));
        });
        Assert.That(await _sut.GetAsync(presetId, userId), Is.Not.Null, "the revived share grants access again");
    }

    [Test]
    public async Task RemoveAllForPresetAsync_SoftDeletesEveryShareAsADirtyTombstone()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var presetId = Guid.NewGuid();
        await _sut.AddOrUpdateAsync(Make(presetId, Guid.NewGuid()));
        await _sut.AddOrUpdateAsync(Make(presetId, Guid.NewGuid()));

        await _sut.RemoveAllForPresetAsync(presetId);

        var tombstones = (await store.GetAllSharesAsync()).Where(s => s.PresetId == presetId).ToList();
        var stillGranted = await _sut.GetByPresetAsync(presetId);
        Assert.Multiple(() =>
        {
            Assert.That(stillGranted, Is.Empty, "revoked shares no longer grant access");
            Assert.That(tombstones, Has.Count.EqualTo(2), "the rows survive as tombstones so the revocation syncs");
            Assert.That(tombstones.All(t => t.IsDeleted && t.IsDirty), Is.True);
        });
    }
}
