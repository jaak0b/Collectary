using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface ISystemFieldUseCase
{
    Task<IReadOnlyList<SystemField>> GetAllAsync();
    Task<SystemField?> GetByIdAsync(Guid id);
    Task CreateAsync(SystemField field);
    Task UpdateAsync(SystemField field);
    Task ReorderAsync(IReadOnlyList<Guid> orderedIds);
    Task DeleteAsync(Guid id);
}
