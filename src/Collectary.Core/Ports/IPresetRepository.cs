using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IPresetRepository
{
    Task<IReadOnlyList<Preset>> GetAllAsync();
    Task<Preset?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Preset>> GetChildrenAsync(Guid parentId);
    Task AddAsync(Preset preset);
    Task UpdateAsync(Preset preset);
    Task DeleteAsync(Guid id);
    Task UpdateDisplayOrdersAsync(IReadOnlyList<Preset> ordered);
    Task BackfillOwnerlessAsync(Guid ownerId);
}
