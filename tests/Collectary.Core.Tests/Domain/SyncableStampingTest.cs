using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class SyncableStampingTest
{
    [Test]
    public void StampModified_MarksDirtyBumpsRevisionAndSetsUser()
    {
        var user = Guid.NewGuid();
        ISyncable item = new Item { Revision = 4, IsDirty = false };

        item.StampModified(user);

        Assert.Multiple(() =>
        {
            Assert.That(item.IsDirty, Is.True);
            Assert.That(item.Revision, Is.EqualTo(5));
            Assert.That(item.LastModifiedByUserId, Is.EqualTo(user));
        });
    }

    [Test]
    public void StampModified_WhenUserNull_KeepsExistingLastModifiedBy()
    {
        var original = Guid.NewGuid();
        ISyncable item = new Item { Revision = 1, LastModifiedByUserId = original };

        item.StampModified(null);

        Assert.That(item.LastModifiedByUserId, Is.EqualTo(original));
    }

    [Test]
    public void MarkPulled_SetsBaseRevisionToRevisionAndClearsDirty()
    {
        ISyncable item = new Item { Revision = 9, BaseRevision = 4, IsDirty = true };

        item.MarkPulled();

        Assert.Multiple(() =>
        {
            Assert.That(item.BaseRevision, Is.EqualTo(9));
            Assert.That(item.IsDirty, Is.False);
        });
    }

    [Test]
    public void StampDeleted_SetsTombstoneFields()
    {
        var user = Guid.NewGuid();
        ISyncable preset = new Preset { Revision = 2 };

        preset.StampDeleted(user);

        Assert.Multiple(() =>
        {
            Assert.That(preset.IsDeleted, Is.True);
            Assert.That(preset.DeletedAt, Is.Not.Null);
            Assert.That(preset.IsDirty, Is.True);
            Assert.That(preset.Revision, Is.EqualTo(3));
            Assert.That(preset.LastModifiedByUserId, Is.EqualTo(user));
        });
    }
}
