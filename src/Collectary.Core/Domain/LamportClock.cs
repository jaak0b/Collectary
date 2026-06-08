namespace Collectary.Core.Domain;

public sealed class LamportClock
{
    public long Next(long current, long observed) => Math.Max(current, observed) + 1;

    public int Compare(SyncVersion a, SyncVersion b) => a.CompareTo(b);
}
