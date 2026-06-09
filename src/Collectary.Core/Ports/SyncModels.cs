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

public record SyncResult(int Pushed, int Pulled);
