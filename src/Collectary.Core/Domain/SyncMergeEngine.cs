namespace Collectary.Core.Domain;

public sealed record MergeCandidate<T>(Guid Id, SyncVersion Version, T Payload);

public sealed class SyncMergeEngine
{
    public IReadOnlyList<MergeCandidate<T>> ResolveWinners<T>(
        IEnumerable<MergeCandidate<T>> candidates,
        ISet<Guid> deletedIds)
    {
        var winners = new Dictionary<Guid, MergeCandidate<T>>();
        foreach (var candidate in candidates)
        {
            if (deletedIds.Contains(candidate.Id)) continue;
            if (!winners.TryGetValue(candidate.Id, out var current)
                || candidate.Version.CompareTo(current.Version) > 0)
                winners[candidate.Id] = candidate;
        }

        return winners.Values.OrderBy(w => w.Id).ToList();
    }
}
