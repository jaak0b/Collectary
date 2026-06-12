namespace Collectary.Core.Domain;

public sealed class LamportClock
{
    public long Next(long current, long observed) => Math.Max(current, observed) + 1;
}
