using Collectary.Search;
using Collectary.Search.Avalonia;
using Collectary.Search.Avalonia.ViewModels;

namespace Collectary.Search.Avalonia.Tests;

file sealed class KeyLocalization : ILocalizationProvider
{
    public string Get(string key) => key;
}

file sealed class FakeCatalog : ISearchUiCatalog
{
    public Task<SearchUiSnapshot> GetSnapshotAsync() => Task.FromResult(new SearchUiSnapshot
    {
        Fields =
        [
            new SearchUiField("name", [], [], [QueryOperatorKind.Contains]),
            new SearchUiField("Status", [], ["open", "done"],
                [QueryOperatorKind.Equals, QueryOperatorKind.In]),
        ],
    });
}

file sealed record Widget(string Name);

file sealed class FakeRunner : ISearchRunner
{
    private readonly IReadOnlyList<object> _items;
    public FakeRunner(params Widget[] items) => _items = items;

    public Task<SearchOutcome> SearchAsync(string queryText) =>
        Task.FromResult(new SearchOutcome(_items, [], []));
}

[TestFixture]
public class StandaloneUsageTest
{
    private readonly ILocalizationProvider _loc = new KeyLocalization();

    [Test]
    public async Task BasicFilter_BuildsQueryText_WithNoCollectaryTypes()
    {
        var runs = new List<string>();
        var vm = new BasicFilterViewModel(
            new FakeCatalog(), _loc, text => { runs.Add(text); return Task.CompletedTask; },
            debounceMilliseconds: 0);
        await vm.LoadAsync();

        Assert.That(vm.AddableFields, Is.EquivalentTo(new[] { "Status" }));

        vm.SearchText = "loco";
        if (vm.PendingRun is { } pending) await pending;

        Assert.That(runs[^1], Is.EqualTo("name ~ loco"));
    }

    [Test]
    public async Task BasicFilter_ChoiceChip_SerializesToAnInClause()
    {
        var runs = new List<string>();
        var vm = new BasicFilterViewModel(
            new FakeCatalog(), _loc, text => { runs.Add(text); return Task.CompletedTask; },
            debounceMilliseconds: 0);
        await vm.LoadAsync();

        vm.AddChipCommand.Execute("Status");
        var chip = vm.Chips.Single();
        chip.VisibleOptions.First(o => o.Value == "open").IsChecked = true;
        chip.VisibleOptions.First(o => o.Value == "done").IsChecked = true;
        if (vm.PendingRun is { } pending) await pending;

        Assert.That(runs[^1], Is.EqualTo("Status in (open, done)"));
    }

    [Test]
    public void FilterChip_ChoiceSelection_ReadsThroughTheLocalizationSeam()
    {
        var chip = new FilterChipViewModel("Status", ["open", "done"], QueryOperatorKind.Equals, _loc, () => { });

        Assert.That(chip.DisplayText, Is.EqualTo("Status: SearchAllValues"));
        chip.VisibleOptions.First(o => o.Value == "open").IsChecked = true;
        Assert.That(chip.DisplayText, Is.EqualTo("Status: open"));
    }

    [Test]
    public async Task ItemQuery_RunsThroughASearchRunner_AndEmitsTypedResults()
    {
        SearchOutcome? applied = null;
        var vm = new ItemQueryViewModel(
            new FakeRunner(new Widget("a"), new Widget("b")),
            new FakeCatalog(),
            new QuerySuggestionEngine(new QueryLexer()),
            _loc,
            outcome => { applied = outcome; return Task.CompletedTask; });

        vm.QueryText = "name ~ a";
        await vm.RunCommand.ExecuteAsync(null);

        Assert.That(applied!.Items.OfType<Widget>().Select(w => w.Name), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task ItemQuery_Suggestions_ComeFromTheCatalog()
    {
        var vm = new ItemQueryViewModel(
            new FakeRunner(), new FakeCatalog(),
            new QuerySuggestionEngine(new QueryLexer()), _loc,
            _ => Task.CompletedTask);

        vm.QueryText = "Sta";
        vm.CaretIndex = 3;
        await vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        Assert.That(vm.Suggestions.Select(s => s.Text), Does.Contain("Status"));
    }
}

[TestFixture]
public class SearchBarViewModelTest
{
    private readonly ILocalizationProvider _loc = new KeyLocalization();

    private async Task<SearchBarViewModel> Make(bool basicPreference, List<bool> saved)
    {
        var query = new ItemQueryViewModel(
            new FakeRunner(), new FakeCatalog(), new QuerySuggestionEngine(new QueryLexer()), _loc,
            _ => Task.CompletedTask);
        var basic = new BasicFilterViewModel(
            new FakeCatalog(), _loc, _ => Task.CompletedTask, debounceMilliseconds: 0);
        var bar = new SearchBarViewModel(query, basic, _loc, () => basicPreference, on => saved.Add(on));
        await basic.LoadAsync();
        return bar;
    }

    [Test]
    public async Task Initialize_WithBasicPreferenceAndRepresentableQuery_StartsInBasicMode()
    {
        var bar = await Make(basicPreference: true, saved: []);

        await bar.InitializeAsync("Status = open");

        Assert.That(bar.IsBasicMode, Is.True);
        Assert.That(bar.BasicFilter.Chips.Single().Label, Is.EqualTo("Status"));
    }

    [Test]
    public async Task Initialize_WithAdvancedPreference_StaysInAdvancedMode()
    {
        var bar = await Make(basicPreference: false, saved: []);

        await bar.InitializeAsync("Status = open");

        Assert.That(bar.IsBasicMode, Is.False);
        Assert.That(bar.Query.QueryText, Is.EqualTo("Status = open"));
    }

    [Test]
    public async Task SwitchToBasic_TooComplexQuery_StaysAdvancedAndMessages()
    {
        var saved = new List<bool>();
        var bar = await Make(basicPreference: false, saved);
        bar.Query.QueryText = "Status = open OR Status = done";

        bar.SwitchToBasicCommand.Execute(null);

        Assert.That(bar.IsBasicMode, Is.False);
        Assert.That(bar.Query.QueryMessage, Is.EqualTo("SearchTooComplexForBasic"));
        Assert.That(saved, Is.Empty);
    }

    [Test]
    public async Task SwitchToAdvanced_SerializesTheBarAndPersistsThePreference()
    {
        var saved = new List<bool>();
        var bar = await Make(basicPreference: true, saved);
        await bar.InitializeAsync("Status = open");

        bar.SwitchToAdvancedCommand.Execute(null);

        Assert.That(bar.IsBasicMode, Is.False);
        Assert.That(bar.Query.QueryText, Is.EqualTo("Status = open"));
        Assert.That(saved, Is.EqualTo(new[] { false }));
    }

    [Test]
    public async Task LocalizedStrings_ComeStraightFromTheProvider()
    {
        var bar = await Make(basicPreference: false, saved: []);

        Assert.Multiple(() =>
        {
            Assert.That(bar.SearchPlaceholder, Is.EqualTo("SearchPlaceholder"));
            Assert.That(bar.SearchLabel, Is.EqualTo("Search"));
            Assert.That(bar.SwitchToBasicLabel, Is.EqualTo("SearchSwitchToBasic"));
            Assert.That(bar.SwitchToAdvancedLabel, Is.EqualTo("SearchSwitchToAdvanced"));
            Assert.That(bar.ItemsPlaceholder, Is.EqualTo("SearchItemsPlaceholder"));
            Assert.That(bar.MoreLabel, Is.EqualTo("SearchMore"));
            Assert.That(bar.FindFieldsPlaceholder, Is.EqualTo("SearchFindFields"));
            Assert.That(bar.SortByLabel, Is.EqualTo("SearchSortBy"));
            Assert.That(bar.SortNoneLabel, Is.EqualTo("SearchSortNone"));
            Assert.That(bar.SortAscendingLabel, Is.EqualTo("SearchSortAscending"));
            Assert.That(bar.SortDescendingLabel, Is.EqualTo("SearchSortDescending"));
        });
    }

    [Test]
    public void Chip_LocalizedStrings_ComeStraightFromTheProvider()
    {
        var chip = new FilterChipViewModel("Status", ["open"], QueryOperatorKind.Contains, _loc, () => { });

        Assert.Multiple(() =>
        {
            Assert.That(chip.OperatorHint, Is.EqualTo("SearchContainsLabel"));
            Assert.That(chip.ValueSearchPlaceholder, Is.EqualTo("SearchFindValues"));
            Assert.That(chip.ValuePlaceholder, Is.EqualTo("SearchValuePlaceholder"));
            Assert.That(chip.ClearLabel, Is.EqualTo("SearchClear"));
            Assert.That(chip.RemoveLabel, Is.EqualTo("SearchRemoveFilter"));
        });
    }

    [Test]
    public async Task RefreshLocalization_RaisesTheLocalizedStringProperties()
    {
        var bar = await Make(basicPreference: false, saved: []);
        var raised = new List<string?>();
        bar.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        Assert.That(bar.SearchLabel, Is.EqualTo("Search"));
        bar.RefreshLocalization();

        Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.SearchLabel)));
        Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.SortByLabel)));
    }
}
