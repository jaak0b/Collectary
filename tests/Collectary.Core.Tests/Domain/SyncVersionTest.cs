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
    public void CompareTo_OrdersByLamportFirst()
    {
        var device = Guid.NewGuid();

        Assert.Multiple(() =>
        {
            Assert.That(new SyncVersion(5, device).CompareTo(new SyncVersion(6, device)), Is.LessThan(0));
            Assert.That(new SyncVersion(7, device).CompareTo(new SyncVersion(6, device)), Is.GreaterThan(0));
        });
    }

    [Test]
    public void CompareTo_OnLamportTie_BreaksByDeviceIdDeterministically()
    {
        var lo = new Guid("00000000-0000-0000-0000-000000000001");
        var hi = new Guid("00000000-0000-0000-0000-000000000002");

        var forward = new SyncVersion(5, lo).CompareTo(new SyncVersion(5, hi));
        var backward = new SyncVersion(5, hi).CompareTo(new SyncVersion(5, lo));

        Assert.Multiple(() =>
        {
            Assert.That(forward, Is.Not.EqualTo(0), "a Lamport tie must still resolve");
            Assert.That(Math.Sign(forward), Is.EqualTo(-Math.Sign(backward)), "the order must be antisymmetric");
        });
    }

    [Test]
    public void CompareTo_IsTotalOrder_DistinctVersionsNeverTie_IdenticalVersionsAreEqual()
    {
        var a = new SyncVersion(5, Guid.NewGuid());
        var b = new SyncVersion(5, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(a.CompareTo(b), Is.Not.EqualTo(0), "two devices at the same Lamport must not tie");
            Assert.That(a.CompareTo(a), Is.EqualTo(0), "an identical version compares equal");
        });
    }

    [Test]
    public void CompareTo_IsTransitive()
    {
        var device = Guid.NewGuid();
        var low = new SyncVersion(1, device);
        var mid = new SyncVersion(2, device);
        var high = new SyncVersion(3, device);

        Assert.Multiple(() =>
        {
            Assert.That(low.CompareTo(mid), Is.LessThan(0));
            Assert.That(mid.CompareTo(high), Is.LessThan(0));
            Assert.That(low.CompareTo(high), Is.LessThan(0));
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
