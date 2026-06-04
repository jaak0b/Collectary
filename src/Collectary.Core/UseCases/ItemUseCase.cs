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
    private readonly ICollectionAuthorization? _authorization;

    public ItemUseCase(IItemRepository items, IPresetUseCase presets, IAppLogger? logger = null, ICollectionAuthorization? authorization = null)
    {
        _items = items;
        _presets = presets;
        _logger = logger ?? new NullAppLogger();
        _authorization = authorization;
    }

    public Task<IReadOnlyList<Item>> GetItemsForPresetAsync(Guid presetId) =>
        _items.GetByPresetAsync(presetId);

    public Task<Item?> GetItemAsync(Guid id) =>
        _items.GetByIdAsync(id);

    public async Task CreateItemAsync(Item item)
    {
        await EnsureCanWriteAsync(item.PresetId);
        var effectiveFields = await _presets.GetEffectiveFieldsAsync(item.PresetId);
        EnsureRequiredFieldsPresent(item, effectiveFields.Fields);
        item.UpdatedAt = DateTime.UtcNow;
        await _items.AddAsync(item);
        _logger.Debug("Created item id={ItemId} preset={PresetId} values={ValueCount}",
            item.Id, item.PresetId, item.Values.Count);
    }

    public async Task UpdateItemAsync(Item item)
    {
        await EnsureCanWriteAsync(item.PresetId);
        var effectiveFields = await _presets.GetEffectiveFieldsAsync(item.PresetId);
        EnsureRequiredFieldsPresent(item, effectiveFields.Fields);
        item.UpdatedAt = DateTime.UtcNow;
        await _items.UpdateAsync(item);
        _logger.Debug("Updated item id={ItemId} preset={PresetId} values={ValueCount}",
            item.Id, item.PresetId, item.Values.Count);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        if (_authorization is not null)
        {
            var existing = await _items.GetByIdAsync(id);
            if (existing is not null) await EnsureCanWriteAsync(existing.PresetId);
        }

        _logger.Debug("Deleting item id={ItemId}", id);
        await _items.DeleteAsync(id);
    }

    private async Task EnsureCanWriteAsync(Guid presetId)
    {
        if (_authorization is not null && !await _authorization.CanWriteAsync(presetId))
            throw new UnauthorizedAccessException("You do not have edit access to this collection.");
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
