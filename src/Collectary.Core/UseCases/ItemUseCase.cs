using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class ItemUseCase : IItemUseCase
{
    private readonly IItemRepository _items;
    private readonly IPresetUseCase _presets;
    private readonly IAppLogger _logger;

    public ItemUseCase(IItemRepository items, IPresetUseCase presets, IAppLogger? logger = null)
    {
        _items = items;
        _presets = presets;
        _logger = logger ?? new NullAppLogger();
    }

    public Task<IReadOnlyList<Item>> GetItemsForPresetAsync(Guid presetId) =>
        _items.GetByPresetAsync(presetId);

    public Task<Item?> GetItemAsync(Guid id) =>
        _items.GetByIdAsync(id);

    public async Task CreateItemAsync(Item item)
    {
        var effectiveFields = await _presets.GetEffectiveFieldsAsync(item.PresetId);
        EnsureRequiredFieldsPresent(item, effectiveFields.Fields);
        item.UpdatedAt = DateTime.UtcNow;
        await _items.AddAsync(item);
        _logger.Debug("Created item id={ItemId} preset={PresetId} values={ValueCount}",
            item.Id, item.PresetId, item.Values.Count);
    }

    public async Task UpdateItemAsync(Item item)
    {
        var effectiveFields = await _presets.GetEffectiveFieldsAsync(item.PresetId);
        EnsureRequiredFieldsPresent(item, effectiveFields.Fields);
        item.UpdatedAt = DateTime.UtcNow;
        await _items.UpdateAsync(item);
        _logger.Debug("Updated item id={ItemId} preset={PresetId} values={ValueCount}",
            item.Id, item.PresetId, item.Values.Count);
    }

    public Task DeleteItemAsync(Guid id)
    {
        _logger.Debug("Deleting item id={ItemId}", id);
        return _items.DeleteAsync(id);
    }

    private static void EnsureRequiredFieldsPresent(Item item, IReadOnlyList<FieldDefinition> effectiveFields)
    {
        var missingRequired = effectiveFields
            .Where(f => f.IsRequired && !f.IsTitleField)
            .Where(f => !item.Values.Any(v => v.FieldDefinitionId == f.Id && !v.IsEmpty))
            .Select(f => f.Label)
            .ToList();

        if (effectiveFields.Any(f => f.IsTitleField && f.IsRequired)
            && string.IsNullOrWhiteSpace(item.DisplayName))
            missingRequired.Insert(0, "Display Name");

        if (missingRequired.Count > 0)
            throw new InvalidOperationException(
                $"Required fields missing: {string.Join(", ", missingRequired)}");
    }
}
