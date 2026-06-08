using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class LamportClockTest
{
    private readonly LamportClock _clock = new();

    [Test]
    public void Next_IsOneAboveTheHigherOfCurrentAndObserved()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_clock.Next(5, 3), Is.EqualTo(6));
            Assert.That(_clock.Next(3, 5), Is.EqualTo(6));
            Assert.That(_clock.Next(0, 0), Is.EqualTo(1));
        });
    }

    [Test]
    public void Compare_OrdersByLamportFirst()
    {
        var device = Guid.NewGuid();

        Assert.Multiple(() =>
        {
            Assert.That(_clock.Compare(new SyncVersion(5, device), new SyncVersion(6, device)), Is.LessThan(0));
            Assert.That(_clock.Compare(new SyncVersion(7, device), new SyncVersion(6, device)), Is.GreaterThan(0));
        });
    }

    [Test]
    public void Compare_OnLamportTie_BreaksByDeviceIdDeterministically()
    {
        var lo = new Guid("00000000-0000-0000-0000-000000000001");
        var hi = new Guid("00000000-0000-0000-0000-000000000002");

        var forward = _clock.Compare(new SyncVersion(5, lo), new SyncVersion(5, hi));
        var backward = _clock.Compare(new SyncVersion(5, hi), new SyncVersion(5, lo));

        Assert.Multiple(() =>
        {
            Assert.That(forward, Is.Not.EqualTo(0), "a Lamport tie must still resolve");
            Assert.That(Math.Sign(forward), Is.EqualTo(-Math.Sign(backward)), "the order must be antisymmetric");
        });
    }

    [Test]
    public void Compare_IsTotalOrder_DistinctVersionsNeverTie_IdenticalVersionsAreEqual()
    {
        var a = new SyncVersion(5, Guid.NewGuid());
        var b = new SyncVersion(5, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(_clock.Compare(a, b), Is.Not.EqualTo(0), "two devices at the same Lamport must not tie");
            Assert.That(_clock.Compare(a, a), Is.EqualTo(0), "an identical version compares equal");
        });
    }

    [Test]
    public void Compare_IsTransitive()
    {
        var device = Guid.NewGuid();
        var low = new SyncVersion(1, device);
        var mid = new SyncVersion(2, device);
        var high = new SyncVersion(3, device);

        Assert.Multiple(() =>
        {
            Assert.That(_clock.Compare(low, mid), Is.LessThan(0));
            Assert.That(_clock.Compare(mid, high), Is.LessThan(0));
            Assert.That(_clock.Compare(low, high), Is.LessThan(0));
        });
    }
}
