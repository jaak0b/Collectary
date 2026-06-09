namespace Collectary.Core.Domain;

public class SyncState
{
    public int Id { get; set; } = 1;
    public long MaxObservedLamport { get; set; }
}
