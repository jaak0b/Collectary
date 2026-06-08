using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class SyncVersionTest
{
    [Test]
    public void Equality_SameLamportAndDevice_AreEqualAndShareHashCode()
    {
        var device = Guid.NewGuid();
        var a = new SyncVersion(7, device);
        var b = new SyncVersion(7, device);

        Assert.Multiple(() =>
        {
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        });
    }

    [Test]
    public void Equality_DifferentLamport_AreNotEqual()
    {
        var device = Guid.NewGuid();

        var a = new SyncVersion(7, device);
        var b = new SyncVersion(8, device);

        Assert.Multiple(() =>
        {
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a == b, Is.False);
            Assert.That(a != b, Is.True);
        });
    }

    [Test]
    public void Equality_DifferentDevice_AreNotEqual()
    {
        var a = new SyncVersion(7, Guid.NewGuid());
        var b = new SyncVersion(7, Guid.NewGuid());

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void UsableAsDictionaryKey_DistinguishesByBothFields()
    {
        var device = Guid.NewGuid();
        var map = new Dictionary<SyncVersion, string>
        {
            [new SyncVersion(1, device)] = "one",
            [new SyncVersion(2, device)] = "two",
        };

        Assert.Multiple(() =>
        {
            Assert.That(map[new SyncVersion(1, device)], Is.EqualTo("one"));
            Assert.That(map[new SyncVersion(2, device)], Is.EqualTo("two"));
            Assert.That(map.ContainsKey(new SyncVersion(1, Guid.NewGuid())), Is.False);
        });
    }

    [Test]
    public void Deconstruct_ExposesLamportAndDevice()
    {
        var device = Guid.NewGuid();

        var (lamport, deviceId) = new SyncVersion(42, device);

        Assert.Multiple(() =>
        {
            Assert.That(lamport, Is.EqualTo(42));
            Assert.That(deviceId, Is.EqualTo(device));
        });
    }
}
