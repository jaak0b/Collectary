using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IPresetUseCase
{
    Task<IReadOnlyList<Preset>> GetAllPresetsAsync();
    Task<IReadOnlyList<Preset>> GetWritablePresetsAsync();
    Task<Preset?> GetPresetAsync(Guid id);
    Task<IReadOnlyList<Preset>> GetChildPresetsAsync(Guid parentId);
    Task<EffectiveFields> GetEffectiveFieldsAsync(Guid presetId);
    Task CreatePresetAsync(Preset preset);
    Task UpdatePresetAsync(Preset preset);
    Task UpdatePresetOrderAsync(IReadOnlyList<Preset> ordered);
    Task DeletePresetAsync(Guid id);
}
