using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class AccountBootstrapper : IAccountBootstrapper
{
    private readonly IPresetRepository _presets;

    public AccountBootstrapper(IPresetRepository presets)
    {
        _presets = presets;
    }

    public Task BackfillOwnerlessAsync(Guid ownerId) =>
        _presets.BackfillOwnerlessAsync(ownerId);
}
