using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface ISearchFieldCatalog
{
    Task<SearchCatalogSnapshot> GetSnapshotAsync();
}

public sealed record SearchFieldGroup(string Label, IReadOnlyList<FieldDefinition> Definitions);

public sealed record SearchPresetEntry(Guid Id, string Name);

public sealed class SearchCatalogSnapshot
{
    public IReadOnlyList<SearchFieldGroup> Fields { get; init; } = [];
    public IReadOnlyList<SearchPresetEntry> Presets { get; init; } = [];

    public SearchFieldGroup? FindField(string label) =>
        Fields.FirstOrDefault(g => string.Equals(g.Label, label, StringComparison.OrdinalIgnoreCase));
}
