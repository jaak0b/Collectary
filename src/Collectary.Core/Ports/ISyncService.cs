namespace Collectary.Core.Ports;

public interface ISyncService
{
    Task<SyncResult> SyncAsync();
    Task ResolveAsync(SyncConflict conflict, bool keepLocal);
}
