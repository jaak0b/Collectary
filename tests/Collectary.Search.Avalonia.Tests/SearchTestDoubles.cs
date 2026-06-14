using Collectary.Search;
using Collectary.Search.ViewModels;

namespace Collectary.Search.Avalonia.Tests;

internal sealed class KeyLocalization : ILocalizationProvider
{
    public string Get(string key) => key;
}

internal sealed class FakeCatalog : ISearchUiCatalog
{
    public Task<SearchUiSnapshot> GetSnapshotAsync() => Task.FromResult(new SearchUiSnapshot
    {
        Fields =
        [
            new SearchUiField("name", [], [], [QueryOperatorKind.Contains]),
            new SearchUiField("Status", [], ["open", "done"], [QueryOperatorKind.Equals, QueryOperatorKind.In]),
        ],
    });
}

internal sealed record Widget(string Name);

internal sealed class FakeRunner : ISearchRunner
{
    private readonly IReadOnlyList<object> _items;

    public FakeRunner(params Widget[] items) => _items = items;

    public Task<SearchOutcome> SearchAsync(string queryText) =>
        Task.FromResult(new SearchOutcome(_items, [], []));
}
