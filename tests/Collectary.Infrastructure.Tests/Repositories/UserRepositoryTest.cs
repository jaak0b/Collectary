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
    public async Task DeleteAsync_HardDeletesRowAndRecordsTombstone()
    {
        var sut = new UserRepository(DbFactory);
        var user = new User { Username = "gone", DisplayName = "Gone" };
        await sut.AddAsync(user);

        await sut.DeleteAsync(user.Id);

        var visible = await sut.GetByIdAsync(user.Id);
        var store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
        var rows = (await store.GetAllUsersAsync()).Where(u => u.Id == user.Id).ToList();
        var tombstones = await store.GetTombstoneIdsAsync();
        Assert.Multiple(() =>
        {
            Assert.That(visible, Is.Null);
            Assert.That(rows, Is.Empty, "the profile row is hard-deleted, leaving no data on disk");
            Assert.That(tombstones, Does.Contain(user.Id), "a tombstone records the deletion so it syncs");
        });
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
