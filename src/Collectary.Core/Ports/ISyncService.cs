namespace Collectary.Core.Ports;

public interface ISyncService
{
    Task<SyncResult> SyncAsync();
}
