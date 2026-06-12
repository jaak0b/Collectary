using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class SearchFieldCatalog : ISearchFieldCatalog
{
    private readonly IPresetUseCase _presets;

    public SearchFieldCatalog(IPresetUseCase presets) => _presets = presets;

    public async Task<SearchCatalogSnapshot> GetSnapshotAsync()
    {
        var presets = await _presets.GetAllPresetsAsync();
        var groups = new Dictionary<string, List<FieldDefinition>>(StringComparer.OrdinalIgnoreCase);
        var seenDefinitionIds = new HashSet<Guid>();
        foreach (var preset in presets)
        {
            var effective = await _presets.GetEffectiveFieldsAsync(preset.Id);
            foreach (var field in effective.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Label)) continue;
                if (!seenDefinitionIds.Add(field.Id)) continue;
                if (!groups.TryGetValue(field.Label, out var definitions))
                {
                    definitions = new List<FieldDefinition>();
                    groups[field.Label] = definitions;
                }
                definitions.Add(field);
            }
        }
        return new SearchCatalogSnapshot
        {
            Fields = groups.Select(g => new SearchFieldGroup(g.Key, g.Value)).ToList(),
            Presets = presets.Select(p => new SearchPresetEntry(p.Id, p.Name)).ToList(),
        };
    }
}
