using System.ComponentModel;
using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Search;
using Collectary.Search.ViewModels;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SearchBarViewModelTest
{
    private sealed record Harness(SearchBarViewModel Bar, BasicFilterViewModel Basic);

    private BasicFilterViewModel _basic = null!;
    private SearchBarViewModel _vm = null!;

    private Harness Build()
    {
        var loc = new LocalizationProvider();
        var catalog = A.Fake<ISearchFieldCatalog>();
        var status = new SingleChoiceFieldDefinition { Label = "Status" };
        status.Choices.Add(new ChoiceOption { Value = "open" });
        A.CallTo(() => catalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Fields = [new SearchFieldGroup("Status", [status])],
        });
        var uiCatalog = new CollectarySearchUiCatalog(catalog);
        var basic = new BasicFilterViewModel(uiCatalog, loc, _ => Task.CompletedTask, debounceMilliseconds: 0);
        var query = new ItemQueryViewModel(
            new ItemSearchRunner(A.Fake<IItemSearchService>()), uiCatalog,
            new QuerySuggestionEngine(new QueryLexer()), loc, _ => Task.CompletedTask);
        return new Harness(new SearchBarViewModel(query, basic, loc, () => true, _ => { }), basic);
    }

    [SetUp]
    public async Task SetUp()
    {
        var harness = Build();
        _vm = harness.Bar;
        _basic = harness.Basic;
        await _basic.LoadAsync();
    }

    [Test]
    public void SortSummary_NoFieldSelected_IsJustTheSortByLabel()
    {
        _basic.SelectedSortField = null;

        Assert.That(_vm.SortSummary, Is.EqualTo(_vm.SortByLabel));
    }

    [Test]
    public void SortSummary_AscendingField_ShowsTheFieldAndAnUpArrow()
    {
        _basic.SelectedSortField = "Status";
        _basic.SortDescending = false;

        Assert.That(_vm.SortSummary, Is.EqualTo($"{_vm.SortByLabel}: Status ↑"));
    }

    [Test]
    public void SortSummary_DescendingField_ShowsTheFieldAndADownArrow()
    {
        _basic.SelectedSortField = "Status";
        _basic.SortDescending = true;

        Assert.That(_vm.SortSummary, Is.EqualTo($"{_vm.SortByLabel}: Status ↓"));
    }

    [Test]
    public void SortSummary_RaisesPropertyChanged_WhenSortFieldChanges()
    {
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _basic.SelectedSortField = "Status";

        Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.SortSummary)));
    }

    [Test]
    public void SortSummary_RaisesPropertyChanged_WhenSortDirectionChanges()
    {
        _basic.SelectedSortField = "Status";
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _basic.SortDescending = true;

        Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.SortSummary)));
    }

    [Test]
    public void SortSummary_RaisesPropertyChanged_WhenSortFieldChangesBetweenTwoFields()
    {
        _basic.SelectedSortField = "Status";
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _basic.SelectedSortField = "name";

        Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.SortSummary)));
    }

    [Test]
    public void RefreshLocalization_RaisesPropertyChanged_ForSortSummary()
    {
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _vm.RefreshLocalization();

        Assert.That(raised, Does.Contain(nameof(SearchBarViewModel.SortSummary)));
    }

    [Test]
    public async Task InitializeAsync_LoadsTheBasicFilterFieldOptions()
    {
        var harness = Build();

        await harness.Bar.InitializeAsync(string.Empty);

        Assert.That(harness.Basic.SortFieldOptions, Is.Not.Empty);
    }
}
