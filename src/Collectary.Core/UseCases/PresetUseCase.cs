using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class PresetUseCase : IPresetUseCase
{
    private readonly IPresetRepository _presets;
    private readonly IItemRepository _items;
    private readonly IAppLogger _logger;
    private readonly ICollectionAuthorization _authorization;

    public PresetUseCase(IPresetRepository presets, IItemRepository items, ICollectionAuthorization authorization, IAppLogger? logger = null)
    {
        _presets = presets;
        _items = items;
        _authorization = authorization;
        _logger = logger ?? new NullAppLogger();
    }

    public Task<IReadOnlyList<Preset>> GetAllPresetsAsync() =>
        _presets.GetAllAsync();

    public Task<Preset?> GetPresetAsync(Guid id) =>
        _presets.GetByIdAsync(id);

    public Task<IReadOnlyList<Preset>> GetChildPresetsAsync(Guid parentId) =>
        _presets.GetChildrenAsync(parentId);

    public Task<EffectiveFields> GetEffectiveFieldsAsync(Guid presetId) =>
        GetEffectiveFieldsAsync(presetId, new HashSet<Guid>());

    private async Task<EffectiveFields> GetEffectiveFieldsAsync(Guid presetId, HashSet<Guid> visited)
    {
        if (!visited.Add(presetId)) return new EffectiveFields();
        var preset = await _presets.GetByIdAsync(presetId);
        if (preset is null) return new EffectiveFields();

        var groupByFieldId = new Dictionary<Guid, Guid?>();
        var groups = new List<FieldGroup>();

        var fields = new List<FieldDefinition>();
        if (preset.ParentPresetId is not null)
        {
            var parent = await GetEffectiveFieldsAsync(preset.ParentPresetId.Value, visited);
            foreach (var parentField in parent.Fields.Where(f => !f.IsTitleField))
            {
                fields.Add(parentField);
                groupByFieldId[parentField.Id] = parent.GroupByFieldId.GetValueOrDefault(parentField.Id);
            }
            groups.AddRange(parent.Groups);
        }

        groups.AddRange(preset.Groups);

        var ownEntries = preset.Fields
            .Select(f => (Def: f, Order: f.DisplayOrder, GroupId: f.GroupId));
        var systemEntries = preset.SharedFieldRefs
            .Select(r => (Def: r.SharedField.Definition, Order: r.DisplayOrder, GroupId: r.GroupId));
        foreach (var entry in ownEntries.Concat(systemEntries).OrderBy(e => e.Order))
        {
            fields.Add(entry.Def);
            groupByFieldId[entry.Def.Id] = entry.GroupId;
        }

        var result = new EffectiveFields
        {
            Fields = fields,
            Groups = groups.OrderBy(g => g.DisplayOrder).ToList(),
            GroupByFieldId = groupByFieldId,
        };

        _logger.Debug(
            "GetEffectiveFields preset={PresetId} parent={ParentId} fields={FieldCount} groups={GroupCount} grouped={GroupedFieldCount}",
            presetId, preset.ParentPresetId, result.Fields.Count, result.Groups.Count,
            groupByFieldId.Count(kv => kv.Value is not null));

        return result;
    }

    public Task CreatePresetAsync(Preset preset) =>
        _presets.AddAsync(preset);

    public async Task UpdatePresetAsync(Preset preset)
    {
        if (!await _authorization.CanWriteAsync(preset.Id))
            throw new UnauthorizedAccessException("You do not have edit access to this collection.");
        await _presets.UpdateAsync(preset);
    }

    public Task UpdatePresetOrderAsync(IReadOnlyList<Preset> ordered) =>
        _presets.UpdateDisplayOrdersAsync(ordered);

    public async Task DeletePresetAsync(Guid id)
    {
        if (!await _authorization.IsOwnerAsync(id))
            throw new UnauthorizedAccessException("Only the owner can delete this collection.");
        await _items.DeleteByPresetAsync(id);
        await _presets.DeleteAsync(id);
    }
}
