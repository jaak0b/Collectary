using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IShareUseCase
{
    Task ShareAsync(Guid presetId, string targetUsername, SharePermission permission);
    Task RevokeAsync(Guid presetId, string targetUsername);
    Task TransferAsync(Guid presetId, string newOwnerUsername);
    Task<IReadOnlyList<ShareInfo>> ListSharesAsync(Guid presetId);
    Task<IReadOnlyList<Preset>> ListSharedWithMeAsync();
}
