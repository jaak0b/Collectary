namespace Collectary.Core.Ports;

public interface ISyncStatus
{
    bool IsConfigured { get; }

    string LocationLabel { get; }
}
