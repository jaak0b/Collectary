namespace Collectary.Core.Ports;

public interface IAutoNumberService
{
    Task<IReadOnlyCollection<int>> UsedNumbersAsync(Guid fieldDefinitionId, Guid? excludeItemId);
}
