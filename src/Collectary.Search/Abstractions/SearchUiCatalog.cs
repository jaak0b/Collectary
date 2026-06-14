namespace Collectary.Search;

public sealed record SearchUiField(
    string Label,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> ValueSuggestions,
    IReadOnlyList<QueryOperatorKind> Operators)
{
    public bool MatchesLabel(string label) =>
        string.Equals(Label, label, StringComparison.OrdinalIgnoreCase)
        || Aliases.Any(a => string.Equals(a, label, StringComparison.OrdinalIgnoreCase));
}

public sealed class SearchUiSnapshot
{
    public IReadOnlyList<SearchUiField> Fields { get; init; } = [];

    public SearchUiField? Find(string label) => Fields.FirstOrDefault(f => f.MatchesLabel(label));
}

public interface ISearchUiCatalog
{
    Task<SearchUiSnapshot> GetSnapshotAsync();
}
