using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class SharedFieldUseCase : ISharedFieldUseCase
{
    private readonly ISharedFieldRepository _repo;

    public SharedFieldUseCase(ISharedFieldRepository repo) => _repo = repo;

    public Task<IReadOnlyList<SharedField>> GetAllAsync() => _repo.GetAllAsync();

    public Task<SharedField?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

    public Task CreateAsync(SharedField field) => _repo.AddAsync(field);

    public Task UpdateAsync(SharedField field) => _repo.UpdateAsync(field);

    public Task ReorderAsync(IReadOnlyList<Guid> orderedIds) => _repo.ReorderAsync(orderedIds);

    public Task DeleteAsync(Guid id) => _repo.DeleteAsync(id);
}
