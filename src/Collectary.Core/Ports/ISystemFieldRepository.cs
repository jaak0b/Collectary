using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface ISystemFieldRepository
{
    Task<IReadOnlyList<SystemField>> GetAllAsync();
    Task<SystemField?> GetByIdAsync(Guid id);
    Task AddAsync(SystemField field);
    Task UpdateAsync(SystemField field);
    Task ReorderAsync(IReadOnlyList<Guid> orderedIds);
    Task DeleteAsync(Guid id);
}
