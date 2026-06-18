using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class AutoNumberService : IAutoNumberService
{
    private readonly IItemRepository _items;

    public AutoNumberService(IItemRepository items) => _items = items;

    public Task<IReadOnlyCollection<int>> UsedNumbersAsync(Guid fieldDefinitionId, Guid? excludeItemId) =>
        _items.GetUsedAutoNumbersAsync(fieldDefinitionId, excludeItemId);
}
