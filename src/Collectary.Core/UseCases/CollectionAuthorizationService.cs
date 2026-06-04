using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class CollectionAuthorizationService : ICollectionAuthorization
{
    private readonly IPresetRepository _presets;
    private readonly IShareRepository _shares;
    private readonly ICurrentUser _currentUser;

    public CollectionAuthorizationService(IPresetRepository presets, IShareRepository shares, ICurrentUser currentUser)
    {
        _presets = presets;
        _shares = shares;
        _currentUser = currentUser;
    }

    public async Task<bool> IsOwnerAsync(Guid presetId)
    {
        var preset = await _presets.GetByIdAsync(presetId);
        return preset is not null && preset.OwnerId == _currentUser.UserId;
    }

    public async Task<bool> CanReadAsync(Guid presetId)
    {
        var preset = await _presets.GetByIdAsync(presetId);
        if (preset is null) return false;
        if (preset.OwnerId == _currentUser.UserId) return true;
        return await _shares.GetAsync(presetId, _currentUser.UserId) is not null;
    }

    public async Task<bool> CanWriteAsync(Guid presetId)
    {
        var preset = await _presets.GetByIdAsync(presetId);
        if (preset is null) return false;
        if (preset.OwnerId == _currentUser.UserId) return true;
        var share = await _shares.GetAsync(presetId, _currentUser.UserId);
        return share is not null && share.Permission == SharePermission.Edit;
    }
}
