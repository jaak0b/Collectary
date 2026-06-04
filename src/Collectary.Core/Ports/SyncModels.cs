namespace Collectary.Core.Ports;

public enum SyncEntityKind
{
    Preset,
    Item,
    SystemField,
}

public record SyncConflict(
    SyncEntityKind Kind,
    Guid Id,
    string LocalLabel,
    string RemoteLabel,
    long LocalRevision,
    long RemoteRevision);

public record SyncResult(int Pushed, int Pulled, IReadOnlyList<SyncConflict> Conflicts)
{
    public bool HasConflicts => Conflicts.Count > 0;
}

public record PurgedTombstone(SyncEntityKind Kind, Guid Id);
