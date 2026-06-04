using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class ShareUseCase : IShareUseCase
{
    private readonly IShareRepository _shares;
    private readonly IUserRepository _users;
    private readonly IPresetRepository _presets;
    private readonly ICurrentUser _currentUser;

    public ShareUseCase(IShareRepository shares, IUserRepository users, IPresetRepository presets, ICurrentUser currentUser)
    {
        _shares = shares;
        _users = users;
        _presets = presets;
        _currentUser = currentUser;
    }

    public async Task ShareAsync(Guid presetId, string targetUsername, SharePermission permission)
    {
        await RequireOwnedPresetAsync(presetId);
        var target = await RequireUserAsync(targetUsername);
        if (target.Id == _currentUser.UserId)
            throw new InvalidOperationException("Cannot share a collection with yourself.");

        await _shares.AddOrUpdateAsync(new CollectionShare
        {
            PresetId = presetId,
            SharedWithUserId = target.Id,
            GrantedByUserId = _currentUser.UserId,
            Permission = permission,
        });
    }

    public async Task RevokeAsync(Guid presetId, string targetUsername)
    {
        await RequireOwnedPresetAsync(presetId);
        var target = await RequireUserAsync(targetUsername);
        await _shares.RemoveAsync(presetId, target.Id);
    }

    public async Task TransferAsync(Guid presetId, string newOwnerUsername)
    {
        await RequireOwnedPresetAsync(presetId);
        var newOwner = await RequireUserAsync(newOwnerUsername);
        if (newOwner.Id == _currentUser.UserId)
            throw new InvalidOperationException("You already own this collection.");

        await _presets.TransferOwnershipAsync(presetId, newOwner.Id);
        await _shares.RemoveAsync(presetId, newOwner.Id);
    }

    public async Task<IReadOnlyList<ShareInfo>> ListSharesAsync(Guid presetId)
    {
        await RequireOwnedPresetAsync(presetId);
        var shares = await _shares.GetByPresetAsync(presetId);
        var result = new List<ShareInfo>();
        foreach (var share in shares)
        {
            var user = await _users.GetByIdAsync(share.SharedWithUserId);
            if (user is null) continue;
            result.Add(new ShareInfo(user.Id, user.Username, user.DisplayName, share.Permission));
        }

        return result;
    }

    public async Task<IReadOnlyList<Preset>> ListSharedWithMeAsync()
    {
        var shares = await _shares.GetForUserAsync(_currentUser.UserId);
        var result = new List<Preset>();
        foreach (var share in shares)
        {
            var preset = await _presets.GetByIdAsync(share.PresetId);
            if (preset is not null) result.Add(preset);
        }

        return result;
    }

    private async Task<Preset> RequireOwnedPresetAsync(Guid presetId)
    {
        var preset = await _presets.GetByIdAsync(presetId)
            ?? throw new InvalidOperationException("Collection not found.");
        if (preset.OwnerId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Only the owner can manage sharing for this collection.");
        return preset;
    }

    private async Task<User> RequireUserAsync(string username) =>
        await _users.GetByUsernameAsync(username) ?? throw new UserNotFoundException(username);
}
