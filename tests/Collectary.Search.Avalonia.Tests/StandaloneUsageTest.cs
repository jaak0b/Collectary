using Collectary.Search;
using Collectary.Search.ViewModels;

namespace Collectary.Search.Avalonia.Tests;

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

        Assert.That(raised, Is.SupersetOf(new[]
        {
            nameof(SearchBarViewModel.SearchPlaceholder),
            nameof(SearchBarViewModel.SearchLabel),
            nameof(SearchBarViewModel.SwitchToBasicLabel),
            nameof(SearchBarViewModel.SwitchToAdvancedLabel),
            nameof(SearchBarViewModel.ItemsPlaceholder),
            nameof(SearchBarViewModel.MoreLabel),
            nameof(SearchBarViewModel.FindFieldsPlaceholder),
            nameof(SearchBarViewModel.SortByLabel),
            nameof(SearchBarViewModel.SortNoneLabel),
            nameof(SearchBarViewModel.SortAscendingLabel),
            nameof(SearchBarViewModel.SortDescendingLabel),
            nameof(SearchBarViewModel.FiltersLabel),
        }));
    }

    [Test]
    public async Task RefreshLocalization_AlsoRefreshesEachChip()
    {
        var bar = await Make(basicPreference: true, saved: []);
        await bar.InitializeAsync("Status = open");
        var chip = bar.BasicFilter.Chips.Single();
        var raised = new List<string?>();
        chip.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        bar.RefreshLocalization();

        Assert.That(raised, Does.Contain(nameof(FilterChipViewModel.DisplayText)));
    }

    [Test]
    public async Task ApplyingAFilter_DoesNotRaiseIsSortActive()
    {
        var bar = await Make(basicPreference: true, saved: []);
        await bar.InitializeAsync(string.Empty);
        bar.BasicFilter.AddChipCommand.Execute("Status");
        var raised = new List<string?>();
        bar.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        bar.BasicFilter.Chips.Single().VisibleOptions.First(o => o.Value == "open").IsChecked = true;

        Assert.That(raised, Does.Not.Contain(nameof(SearchBarViewModel.IsSortActive)),
            "applying a value filter must not masquerade as a sort change");
    }

    [Test]
    public async Task ChangingAnUnrelatedFilterProperty_DoesNotRaiseIsSortActive()
    {
        var bar = await Make(basicPreference: true, saved: []);
        await bar.InitializeAsync(string.Empty);
        var raised = new List<string?>();
        bar.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        bar.BasicFilter.SortDescending = !bar.BasicFilter.SortDescending;

        Assert.That(raised, Does.Not.Contain(nameof(SearchBarViewModel.IsSortActive)),
            "only an IsSortActive change on the basic filter may re-publish the bar's IsSortActive");
    }

    [Test]
    public async Task IsFilterPanelExpanded_DefaultsToCollapsed()
    {
        var bar = await Make(basicPreference: true, saved: []);

        Assert.That(bar.IsFilterPanelExpanded, Is.False);
    }

    [Test]
    public async Task FiltersLabel_NoActiveFilters_UsesThePlainLabel()
    {
        var bar = await Make(basicPreference: true, saved: []);
        await bar.InitializeAsync(string.Empty);

        Assert.That(bar.ActiveFilterCount, Is.EqualTo(0));
        Assert.That(bar.FiltersLabel, Is.EqualTo(SearchLocalizationKeys.SearchFilters));
    }

    [Test]
    public async Task FiltersLabel_WithActiveFilters_UsesTheCountedLabelAndSubstitutesTheCount()
    {
        var counting = new CountingLocalization();
        var query = new ItemQueryViewModel(
            new FakeRunner(), new FakeCatalog(), new QuerySuggestionEngine(new QueryLexer()), counting,
            _ => Task.CompletedTask);
        var basic = new BasicFilterViewModel(
            new FakeCatalog(), counting, _ => Task.CompletedTask, debounceMilliseconds: 0);
        var bar = new SearchBarViewModel(query, basic, counting, () => true, _ => { });
        await basic.LoadAsync();
        await bar.InitializeAsync("Status = open");

        Assert.That(bar.ActiveFilterCount, Is.EqualTo(1));
        Assert.That(bar.FiltersLabel, Is.EqualTo("Filters (1)"));
    }

    [Test]
    public async Task ActiveFilterCount_AndFiltersLabel_RaiseWhenAFilterIsApplied()
    {
        var bar = await Make(basicPreference: true, saved: []);
        await bar.InitializeAsync(string.Empty);
        bar.BasicFilter.AddChipCommand.Execute("Status");
        var raised = new List<string?>();
        bar.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        bar.BasicFilter.Chips.Single().VisibleOptions.First(o => o.Value == "open").IsChecked = true;

        Assert.Multiple(() =>
        {
            Assert.That(bar.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.ActiveFilterCount)));
            Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.FiltersLabel)));
        });
    }

    [Test]
    public async Task IsSortActive_SurfacesFromTheBasicFilter()
    {
        var bar = await Make(basicPreference: true, saved: []);
        await bar.InitializeAsync(string.Empty);
        var raised = new List<string?>();
        bar.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        bar.BasicFilter.SelectedSortField = "name";

        Assert.That(bar.IsSortActive, Is.True);
        Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.IsSortActive)));
    }

    private sealed class CountingLocalization : ILocalizationProvider
    {
        public string Get(string key) => key switch
        {
            SearchLocalizationKeys.SearchFilters => "Filters",
            SearchLocalizationKeys.SearchFiltersWithCount => "Filters ({0})",
            _ => key,
        };
    }
}
