namespace Collectary.Core.Domain;

public readonly record struct SyncVersion(long Lamport, Guid DeviceId) : IComparable<SyncVersion>
{
    public int CompareTo(SyncVersion other)
    {
        var byLamport = Lamport.CompareTo(other.Lamport);
        return byLamport != 0 ? byLamport : DeviceId.CompareTo(other.DeviceId);
    }
}
