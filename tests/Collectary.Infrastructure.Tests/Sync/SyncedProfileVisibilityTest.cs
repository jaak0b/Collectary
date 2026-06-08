using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class SyncedProfileVisibilityTest : DbIntegrationTestBase
{
    private EfSyncStore _store = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _store = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
    }

    private async Task ApplyOwnedPresetAsync(Guid ownerId, Guid presetId, string name)
    {
        await _store.ApplyUserAsync(new User { Id = ownerId, Username = name.ToLowerInvariant(), DisplayName = name, Revision = 1 });
        var preset = new Preset { Id = presetId, Name = name, OwnerId = ownerId, Revision = 1 };
        preset.Fields.Add(new TextFieldDefinition { Label = "Title", PresetId = presetId });
        await _store.ApplyPresetAsync(preset);
    }

    [Test]
    public async Task SyncedForeignOwner_AfterSwitchingToThatProfile_SeesItsCollections()
    {
        var ownerId = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        await ApplyOwnedPresetAsync(ownerId, presetId, "Trains");

        var asOwner = new PresetRepository(DbFactory, new FieldDefinitionMerger(), currentUser: new FixedSyncUser(ownerId));
        var visible = await asOwner.GetAllAsync();

        Assert.That(visible.Select(p => p.Id), Does.Contain(presetId),
            "after the owner profile syncs in, switching to it must surface its collections");
    }

    [Test]
    public async Task SyncedForeignOwner_FromADifferentProfile_StaysHiddenUntilShared()
    {
        var ownerId = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        await ApplyOwnedPresetAsync(ownerId, presetId, "Trains");

        var otherId = Guid.NewGuid();
        var asOther = new PresetRepository(DbFactory, new FieldDefinitionMerger(), currentUser: new FixedSyncUser(otherId));
        var beforeShare = await asOther.GetAllAsync();

        await _store.ApplyShareAsync(new CollectionShare
        {
            Id = Guid.NewGuid(), PresetId = presetId, SharedWithUserId = otherId,
            GrantedByUserId = ownerId, Permission = SharePermission.Read, Revision = 1,
        });
        var afterShare = await asOther.GetAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(beforeShare.Select(p => p.Id), Does.Not.Contain(presetId),
                "a foreign-owned collection is not visible to an unrelated profile");
            Assert.That(afterShare.Select(p => p.Id), Does.Contain(presetId),
                "once a synced share grants access, the collection becomes visible without switching owners");
        });
    }
}

file sealed class FixedSyncUser : ICurrentUser
{
    public FixedSyncUser(Guid userId) => UserId = userId;
    public Guid UserId { get; }
    public bool IsAuthenticated => UserId != Guid.Empty;
}
