using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Search;

namespace Collectary.Core.UseCases;

public class CollectarySearchUiCatalog : ISearchUiCatalog
{
    private readonly ISearchFieldCatalog _catalog;
    private readonly PseudoFieldCatalog _pseudo = new();

    public CollectarySearchUiCatalog(ISearchFieldCatalog catalog) => _catalog = catalog;

    public async Task<SearchUiSnapshot> GetSnapshotAsync()
    {
        var snapshot = await _catalog.GetSnapshotAsync();
        var presetNames = snapshot.Presets.Select(p => p.Name).ToList();
        var fields = _pseudo.Labels
            .Select(label => new SearchUiField(
                label,
                _pseudo.AliasesFor(label),
                _pseudo.SuggestsPresetNames(label) ? presetNames : [],
                _pseudo.OperatorsFor(label)))
            .ToList();
        var pseudoLabels = fields.Select(f => f.Label).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var group in snapshot.Fields)
        {
            if (pseudoLabels.Contains(group.Label)) continue;
            var searchable = group.Definitions.OfType<ISearchableFieldDefinition>().ToList();
            if (searchable.Count == 0) continue;
            fields.Add(new SearchUiField(
                group.Label,
                [],
                searchable.SelectMany(d => d.ValueSuggestions()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                searchable.SelectMany(d => d.SupportedOperators).Distinct().ToList()));
        }
        return new SearchUiSnapshot { Fields = fields };
    }
}
