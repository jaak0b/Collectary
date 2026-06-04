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
}
