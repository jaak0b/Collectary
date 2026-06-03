using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class SystemFieldUseCase : ISystemFieldUseCase
{
    private readonly ISystemFieldRepository _repo;

    public SystemFieldUseCase(ISystemFieldRepository repo) => _repo = repo;

    public Task<IReadOnlyList<SystemField>> GetAllAsync() => _repo.GetAllAsync();

    public Task<SystemField?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

    public Task CreateAsync(SystemField field) => _repo.AddAsync(field);

    public Task UpdateAsync(SystemField field) => _repo.UpdateAsync(field);

    public Task ReorderAsync(IReadOnlyList<Guid> orderedIds) => _repo.ReorderAsync(orderedIds);

    public Task DeleteAsync(Guid id) => _repo.DeleteAsync(id);
}
