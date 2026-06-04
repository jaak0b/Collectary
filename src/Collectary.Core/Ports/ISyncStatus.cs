namespace Collectary.Core.Ports;

public interface ISyncStatus
{
    bool IsConfigured { get; }
    int TombstoneRetentionDays { get; }
}
