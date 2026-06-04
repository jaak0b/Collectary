using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class CollectionShareTest
{
    [Test]
    public void Permission_DefaultsToRead() =>
        Assert.That(new CollectionShare().Permission, Is.EqualTo(SharePermission.Read));

    [Test]
    public void GrantedAt_DefaultsToUtcNow() =>
        Assert.That(new CollectionShare().GrantedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));

    [Test]
    public void Ids_DefaultToEmpty()
    {
        var share = new CollectionShare();

        Assert.Multiple(() =>
        {
            Assert.That(share.PresetId, Is.EqualTo(Guid.Empty));
            Assert.That(share.SharedWithUserId, Is.EqualTo(Guid.Empty));
            Assert.That(share.GrantedByUserId, Is.EqualTo(Guid.Empty));
        });
    }
}
