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

public readonly record struct PushStamp(SyncEntityKind Kind, Guid Id, long Lamport, Guid DeviceId);

public record SyncResult(
    int Pushed,
    int Pulled,
    int Skipped = 0,
    int UnreadableDevices = 0,
    int ImagesFailed = 0,
    bool BackendUnavailable = false);
