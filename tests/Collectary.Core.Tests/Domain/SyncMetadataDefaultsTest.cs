using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class SyncMetadataDefaultsTest
{
    [Test]
    public void Preset_OwnerId_DefaultsToNull() =>
        Assert.That(new Preset().OwnerId, Is.Null);

    [Test]
    public void Preset_ImplementsISyncable() =>
        Assert.That(new Preset(), Is.InstanceOf<ISyncable>());

    [Test]
    public void Item_ImplementsISyncable() =>
        Assert.That(new Item(), Is.InstanceOf<ISyncable>());

    [Test]
    public void Syncable_IsDeleted_DefaultsToFalse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new Preset().IsDeleted, Is.False);
            Assert.That(new Item().IsDeleted, Is.False);
        });
    }

    [Test]
    public void Syncable_DeletedAt_DefaultsToNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new Preset().DeletedAt, Is.Null);
            Assert.That(new Item().DeletedAt, Is.Null);
        });
    }

    [Test]
    public void Syncable_IsDirty_DefaultsToFalse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new Preset().IsDirty, Is.False);
            Assert.That(new Item().IsDirty, Is.False);
        });
    }

    [Test]
    public void Syncable_Revisions_DefaultToZero()
    {
        var preset = new Preset();
        var item = new Item();

        Assert.Multiple(() =>
        {
            Assert.That(preset.Revision, Is.EqualTo(0));
            Assert.That(preset.BaseRevision, Is.EqualTo(0));
            Assert.That(item.Revision, Is.EqualTo(0));
            Assert.That(item.BaseRevision, Is.EqualTo(0));
        });
    }

    [Test]
    public void Syncable_LastModifiedByUserId_DefaultsToNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new Preset().LastModifiedByUserId, Is.Null);
            Assert.That(new Item().LastModifiedByUserId, Is.Null);
        });
    }
}
