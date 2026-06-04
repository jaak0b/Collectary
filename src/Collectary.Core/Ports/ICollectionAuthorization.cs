namespace Collectary.Core.Ports;

public interface ICollectionAuthorization
{
    Task<bool> CanReadAsync(Guid presetId);
    Task<bool> CanWriteAsync(Guid presetId);
    Task<bool> IsOwnerAsync(Guid presetId);
}
