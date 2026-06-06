using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface ISharedFieldRepository
{
    Task<IReadOnlyList<SharedField>> GetAllAsync();
    Task<SharedField?> GetByIdAsync(Guid id);
    Task AddAsync(SharedField field);
    Task UpdateAsync(SharedField field);
    Task ReorderAsync(IReadOnlyList<Guid> orderedIds);
    Task DeleteAsync(Guid id);
}
