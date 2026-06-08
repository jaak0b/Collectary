namespace Collectary.Core.Ports;

public enum SyncEntityKind
{
    Preset,
    Item,
    SharedField,
    User,
    Share,
}

public record SyncConflict(
    SyncEntityKind Kind,
    Guid Id,
    string LocalLabel,
    string RemoteLabel,
    long LocalRevision,
    long RemoteRevision);

public record SyncResult(int Pushed, int Pulled, IReadOnlyList<SyncConflict> Conflicts, int Skipped = 0)
{
    public bool HasConflicts => Conflicts.Count > 0;

    public bool HadProblems => Conflicts.Count > 0 || Skipped > 0;
}

public record PurgedTombstone(SyncEntityKind Kind, Guid Id);
