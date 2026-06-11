using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class SyncMergeEngineTest
{
    private readonly SyncMergeEngine _engine = new();

    private static MergeCandidate<string> Candidate(Guid id, long lamport, Guid device, string payload)
        => new(id, new SyncVersion(lamport, device), payload);

    [Test]
    public void ResolveWinners_PicksHighestLamportPerId()
    {
        var id = Guid.NewGuid();
        var device = Guid.NewGuid();

        var winners = _engine.ResolveWinners(new[]
        {
            Candidate(id, 3, device, "old"),
            Candidate(id, 5, device, "new"),
            Candidate(id, 4, device, "mid"),
        }, new HashSet<Guid>());

        Assert.That(winners, Has.Count.EqualTo(1));
        Assert.That(winners[0].Payload, Is.EqualTo("new"));
    }

    [Test]
    public void ResolveWinners_IsIndependentOfCandidateOrder()
    {
        var id = Guid.NewGuid();
        var device = Guid.NewGuid();
        var forward = new[]
        {
            Candidate(id, 1, device, "a"),
            Candidate(id, 9, device, "winner"),
            Candidate(id, 4, device, "b"),
        };
        var reversed = forward.Reverse().ToArray();

        var a = _engine.ResolveWinners(forward, new HashSet<Guid>());
        var b = _engine.ResolveWinners(reversed, new HashSet<Guid>());

        Assert.That(a[0].Payload, Is.EqualTo("winner"));
        Assert.That(b[0].Payload, Is.EqualTo("winner"));
    }

    [Test]
    public void ResolveWinners_OnLamportTie_BreaksByDeviceIdDeterministically()
    {
        var id = Guid.NewGuid();
        var lo = new Guid("00000000-0000-0000-0000-000000000001");
        var hi = new Guid("00000000-0000-0000-0000-000000000002");

        var winnerVersion = _engine.ResolveWinners(new[]
        {
            Candidate(id, 5, lo, "lo"),
            Candidate(id, 5, hi, "hi"),
        }, new HashSet<Guid>())[0].Version;

        var expected = new SyncVersion(5, lo).CompareTo(new SyncVersion(5, hi)) > 0
            ? lo
            : hi;
        Assert.That(winnerVersion.DeviceId, Is.EqualTo(expected));
    }

    [Test]
    public void ResolveWinners_DeleteWins_ExcludesTombstonedIdEvenWithHighestVersion()
    {
        var deleted = Guid.NewGuid();
        var kept = Guid.NewGuid();
        var device = Guid.NewGuid();

        var winners = _engine.ResolveWinners(new[]
        {
            Candidate(deleted, 100, device, "should-be-gone"),
            Candidate(kept, 2, device, "stays"),
        }, new HashSet<Guid> { deleted });

        Assert.Multiple(() =>
        {
            Assert.That(winners.Select(w => w.Id), Does.Not.Contain(deleted), "a tombstoned id wins the delete");
            Assert.That(winners.Select(w => w.Id), Does.Contain(kept));
        });
    }

    [Test]
    public void ResolveWinners_ResolvesEachIdIndependently()
    {
        var device = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var winners = _engine.ResolveWinners(new[]
        {
            Candidate(first, 1, device, "first-old"),
            Candidate(first, 2, device, "first-new"),
            Candidate(second, 5, device, "second"),
        }, new HashSet<Guid>());

        Assert.Multiple(() =>
        {
            Assert.That(winners.Single(w => w.Id == first).Payload, Is.EqualTo("first-new"));
            Assert.That(winners.Single(w => w.Id == second).Payload, Is.EqualTo("second"));
        });
    }

    [Test]
    public void ResolveWinners_WithNoCandidates_ReturnsEmpty()
    {
        Assert.That(_engine.ResolveWinners(Array.Empty<MergeCandidate<string>>(), new HashSet<Guid>()), Is.Empty);
    }

    [Test]
    public void ResolveWinners_ReturnsWinnersOrderedByIdForReproducibility()
    {
        var device = Guid.NewGuid();
        var low = new Guid("00000000-0000-0000-0000-0000000000aa");
        var high = new Guid("00000000-0000-0000-0000-0000000000bb");

        var winners = _engine.ResolveWinners(new[]
        {
            Candidate(high, 1, device, "high"),
            Candidate(low, 1, device, "low"),
        }, new HashSet<Guid>());

        Assert.That(winners.Select(w => w.Id), Is.EqualTo(new[] { low, high }), "output must be deterministically ordered by id");
    }
}
