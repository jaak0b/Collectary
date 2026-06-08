namespace Collectary.Core.Domain;

public sealed record MergeCandidate<T>(Guid Id, SyncVersion Version, T Payload);

public sealed class SyncMergeEngine
{
    private readonly LamportClock _clock;

    public SyncMergeEngine(LamportClock clock) => _clock = clock;

    public IReadOnlyList<MergeCandidate<T>> ResolveWinners<T>(
        IEnumerable<MergeCandidate<T>> candidates,
        ISet<Guid> deletedIds)
    {
        var winners = new Dictionary<Guid, MergeCandidate<T>>();
        foreach (var candidate in candidates)
        {
            if (deletedIds.Contains(candidate.Id)) continue;
            if (!winners.TryGetValue(candidate.Id, out var current)
                || _clock.Compare(candidate.Version, current.Version) > 0)
                winners[candidate.Id] = candidate;
        }

        return winners.Values.OrderBy(w => w.Id).ToList();
    }
}
