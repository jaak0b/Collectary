using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Repositories;

[TestFixture]
public class UserRepositoryTest : DbIntegrationTestBase
{
    private UserRepository _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new UserRepository(DbFactory);
    }

    [Test]
    public async Task AddAsync_ThenGetByUsername_ReturnsUser()
    {
        var user = new User { Username = "alice", DisplayName = "Alice" };
        await _sut.AddAsync(user);

        var loaded = await _sut.GetByUsernameAsync("alice");

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Id, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task GetByUsernameAsync_WhenMissing_ReturnsNull() =>
        Assert.That(await _sut.GetByUsernameAsync("ghost"), Is.Null);

    [Test]
    public async Task GetByUsernameAsync_IsCaseInsensitive()
    {
        var user = new User { Username = "Alice", DisplayName = "Alice" };
        await _sut.AddAsync(user);

        var loaded = await _sut.GetByUsernameAsync("alice");

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Id, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsUser()
    {
        var user = new User { Username = "bob" };
        await _sut.AddAsync(user);

        var loaded = await _sut.GetByIdAsync(user.Id);

        Assert.That(loaded!.Username, Is.EqualTo("bob"));
    }

    [Test]
    public async Task GetAllAsync_ReturnsAddedUsers()
    {
        await _sut.AddAsync(new User { Username = "a" });
        await _sut.AddAsync(new User { Username = "b" });

        var all = await _sut.GetAllAsync();

        Assert.That(all.Select(u => u.Username), Is.EquivalentTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task DeleteAsync_WhenSyncConfigured_SoftDeletesAsADirtyTombstoneHiddenFromQueries()
    {
        var sut = new UserRepository(DbFactory, new ConfiguredSyncStatus());
        var user = new User { Username = "gone", DisplayName = "Gone" };
        await sut.AddAsync(user);

        await sut.DeleteAsync(user.Id);

        var visible = await sut.GetByIdAsync(user.Id);
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var tombstone = (await store.GetAllUsersAsync()).Single(u => u.Id == user.Id);
        Assert.Multiple(() =>
        {
            Assert.That(visible, Is.Null, "a soft-deleted profile is hidden from normal queries");
            Assert.That(tombstone.IsDeleted, Is.True, "the row survives as a tombstone");
            Assert.That(tombstone.IsDirty, Is.True, "the tombstone must be dirty so the deletion propagates on the next sync");
        });
    }

    [Test]
    public async Task DeleteAsync_WhenSyncNotConfigured_HardDeletes()
    {
        var sut = new UserRepository(DbFactory);
        var user = new User { Username = "gone", DisplayName = "Gone" };
        await sut.AddAsync(user);

        await sut.DeleteAsync(user.Id);

        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        Assert.That((await store.GetAllUsersAsync()).Any(u => u.Id == user.Id), Is.False,
            "with no sync configured there is nothing to propagate, so the row is removed outright");
    }

    private sealed class ConfiguredSyncStatus : ISyncStatus
    {
        public bool IsConfigured => true;
        public int TombstoneRetentionDays => 30;
    }

    [Test]
    public async Task AddAsync_StampsTheProfileDirtySoItSyncs()
    {
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var user = new User { Username = "alice", DisplayName = "Alice" };

        await _sut.AddAsync(user);

        var stored = (await store.GetAllUsersAsync()).Single(u => u.Id == user.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.IsDirty, Is.True, "a new profile must be dirty so the next sync pushes it to the folder");
            Assert.That(stored.Revision, Is.EqualTo(1));
        });
    }
}
