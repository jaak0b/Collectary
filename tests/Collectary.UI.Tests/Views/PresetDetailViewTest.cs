using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Search;
using Collectary.Search.Avalonia.Controls;
using Collectary.Presentation.DI;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.ListCells;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class PresetDetailViewTest
{
    private string _originalPreferencesPath = null!;
    private string _preferencesDir = null!;

    [SetUp]
    public void SetUp()
    {
        _originalPreferencesPath = AppPreferences.FilePath;
        _preferencesDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_preferencesDir);
        AppPreferences.FilePath = Path.Combine(_preferencesDir, "preferences.json");
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _originalPreferencesPath;
        Directory.Delete(_preferencesDir, true);
    }

    private static async Task<PresetDetailViewModel> LoadedVmWithOneColumn()
    {
        var itemUseCase = A.Fake<IItemUseCase>();
        var presetUseCase = A.Fake<IPresetUseCase>();
        var searchService = A.Fake<IItemSearchService>();
        var searchCatalog = A.Fake<ISearchFieldCatalog>();
        var listCellBuilder = A.Fake<IListCellBuilder>();
        var dialogService = A.Fake<IDialogService>();

        var preset = new Preset { Name = "Test" };
        var field = new TextFieldDefinition { Label = "Name", ShowInList = true };
        A.CallTo(() => presetUseCase.GetEffectiveFieldsAsync(preset.Id))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { field } });
        A.CallTo(() => searchService.SearchAsync(A<string>._))
            .Returns(new ItemSearchResult([], [], []));
        A.CallTo(() => searchCatalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot());
        A.CallTo(() => listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);
        A.CallTo(() => listCellBuilder.Build(A<IReadOnlyList<FieldValue>>._, A<IReadOnlyList<FieldDefinition>>._))
            .Returns((IReadOnlyList<ListCellViewModelBase>)new List<ListCellViewModelBase>());

        var vm = new PresetDetailViewModel(preset, itemUseCase, presetUseCase, searchService, searchCatalog,
            listCellBuilder, dialogService,
            navigateToItemEditor: (_, _, _) => { }, navigateBack: () => { });
        await vm.LoadAsync();
        return vm;
    }

    [Test]
    public async Task SuggestionList_NeverTakesFocus_SoClickingASuggestionCannotCloseThePopup()
    {
        var vm = await LoadedVmWithOneColumn();
        var view = new PresetDetailView { DataContext = vm };
        var window = new Window { Content = view };
        window.Show();

        vm.SearchBar.Query.Suggestions.Add(new QuerySuggestion("name", "name", 0, 0, QuerySuggestionKind.Field));
        vm.SearchBar.Query.AreSuggestionsOpen = true;
        Dispatcher.UIThread.RunJobs();

        var list = Bar(view).FindControl<ListBox>("SuggestionList")!;
        var container = list.ContainerFromIndex(0) as ListBoxItem;
        Assert.Multiple(() =>
        {
            Assert.That(list.Focusable, Is.False, "the suggestion list must not steal focus from the search box");
            Assert.That(container, Is.Not.Null, "the suggestion item must be realized inside the open popup");
            Assert.That(container!.Focusable, Is.False, "suggestion items must not steal focus from the search box");
        });
        window.Close();
    }

    [Test]
    public async Task ActionColumn_AppearsExactlyOnce_AsTheLastColumn()
    {
        var vm = await LoadedVmWithOneColumn();

        var view = new PresetDetailView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        var grid = view.FindControl<DataGrid>("ItemGrid")!;

        Assert.Multiple(() =>
        {
            Assert.That(grid.Columns.Count(c => c.Header as string == ""), Is.EqualTo(1), "the ⋯ action column must appear exactly once");
            Assert.That(grid.Columns[^1].Header, Is.EqualTo(""), "the ⋯ action column must be the last column");
        });
    }

    private static async Task<PresetDetailViewModel> LoadedBasicModeVm()
    {
        var itemUseCase = A.Fake<IItemUseCase>();
        var presetUseCase = A.Fake<IPresetUseCase>();
        var searchService = A.Fake<IItemSearchService>();
        var searchCatalog = A.Fake<ISearchFieldCatalog>();
        var listCellBuilder = A.Fake<IListCellBuilder>();
        var dialogService = A.Fake<IDialogService>();

        var preset = new Preset { Name = "Trains" };
        var status = new SingleChoiceFieldDefinition { Label = "Status" };
        status.Choices.Add(new ChoiceOption { Value = "open" });
        A.CallTo(() => presetUseCase.GetEffectiveFieldsAsync(preset.Id)).Returns(new EffectiveFields());
        A.CallTo(() => searchService.SearchAsync(A<string>._)).Returns(new ItemSearchResult([], [], []));
        A.CallTo(() => searchCatalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Fields = [new SearchFieldGroup("Status", [status])],
            Presets = [new SearchPresetEntry(preset.Id, preset.Name)],
        });

        var vm = new PresetDetailViewModel(preset, itemUseCase, presetUseCase, searchService, searchCatalog,
            listCellBuilder, dialogService,
            navigateToItemEditor: (_, _, _) => { }, navigateBack: () => { });
        await vm.LoadAsync();
        return vm;
    }

    private static (PresetDetailView View, Window Window) Show(PresetDetailViewModel vm)
    {
        var view = new PresetDetailView { DataContext = vm };
        var window = new Window { Content = view, Width = 900, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (view, window);
    }

    private static SearchBar Bar(PresetDetailView view) =>
        Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(view).OfType<SearchBar>().First();

    private static IReadOnlyList<Button> ChipButtons(PresetDetailView view) =>
        Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(view)
            .OfType<Button>().Where(b => b.Classes.Contains("chip")).ToList();

    [Test]
    public async Task BasicMode_ShowsTheChipBarAndHidesTheAdvancedBox()
    {
        var vm = await LoadedBasicModeVm();
        var (view, _) = Show(vm);

        var basic = Bar(view).FindControl<Control>("BasicPanel")!;
        var advanced = Bar(view).FindControl<Control>("AdvancedPanel")!;
        Assert.That(vm.SearchBar.IsBasicMode, Is.True);
        Assert.That(basic.IsVisible, Is.True);
        Assert.That(advanced.IsVisible, Is.False);

        vm.SearchBar.SwitchToAdvancedCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.That(basic.IsVisible, Is.False);
        Assert.That(advanced.IsVisible, Is.True);
    }

    [Test]
    public async Task BasicMode_RendersAChipButtonPerChipWithItsDisplayText()
    {
        var vm = await LoadedBasicModeVm();
        var (view, _) = Show(vm);

        var chips = ChipButtons(view);
        Assert.That(chips, Has.Count.EqualTo(1));
        Assert.That(chips[0].Content, Is.EqualTo("collection: Trains"));

        vm.SearchBar.BasicFilter.AddChipCommand.Execute("Status");
        Dispatcher.UIThread.RunJobs();
        Assert.That(ChipButtons(view), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task BasicMode_HasMoreAndSortControls()
    {
        var vm = await LoadedBasicModeVm();
        var (view, _) = Show(vm);

        Assert.That(Bar(view).FindControl<Button>("MoreButton"), Is.Not.Null);
        Assert.That(Bar(view).FindControl<Button>("SortButton"), Is.Not.Null);
        Assert.That(vm.SearchBar.BasicFilter.SortFieldOptions, Is.Not.Empty);
    }

    private static (PresetDetailView View, Window Window) ShowAt(PresetDetailViewModel vm, double width)
    {
        var view = new PresetDetailView { DataContext = vm };
        var window = new Window { Content = view, Width = width, Height = 640 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (view, window);
    }

    private static Rect ViewportRect(Visual control, Visual root)
    {
        var topLeft = control.TranslatePoint(default, root) ?? default;
        return new Rect(topLeft, control.Bounds.Size);
    }

    [Test]
    public async Task BasicMode_OnANarrowWindow_CollapsesFiltersToTheSearchBoxAndAToggle()
    {
        var vm = await LoadedBasicModeVm();
        var (view, window) = ShowAt(vm, 380);

        var basicPanel = Bar(view).FindControl<SearchRowPanel>("BasicPanel")!;
        var trailing = Bar(view).FindControl<Control>("TrailingControls")!;
        var chips = Bar(view).FindControl<Control>("ChipArea")!;
        var toggle = Bar(view).FindControl<Control>("FiltersToggle")!;

        Assert.Multiple(() =>
        {
            Assert.That(basicPanel.IsStacked, Is.True,
                "a narrow viewport must put the basic panel into its stacked layout");
            Assert.That(chips.Opacity, Is.EqualTo(0),
                "collapsed filters hide only the chip area");
            Assert.That(trailing.Opacity, Is.EqualTo(1),
                "the compact sort + advanced controls stay on the top row, never collapsed");
            Assert.That(toggle.Opacity, Is.EqualTo(1),
                "a Filters toggle appears on a narrow window so the collapsed chips can be reopened");
        });
        window.Close();
    }

    [Test]
    public async Task BasicMode_OnANarrowWindow_WhenExpanded_KeepsSortOnTopRowAndStacksOnlyChipsBelow()
    {
        var vm = await LoadedBasicModeVm();
        var (view, window) = ShowAt(vm, 760);

        vm.SearchBar.IsFilterPanelExpanded = true;
        Dispatcher.UIThread.RunJobs();

        var search = Bar(view).FindControl<TextBox>("ItemsSearchBox")!;
        var sortButton = Bar(view).FindControl<Control>("SortButton")!;
        var chips = Bar(view).FindControl<Control>("ChipArea")!;

        Assert.That(ViewportRect(sortButton, window).Left, Is.GreaterThan(ViewportRect(search, window).Right - 0.5),
            "the compact sort button stays to the right of the search box on the top row, not on a row of its own");
        Assert.That(ViewportRect(sortButton, window).Top, Is.LessThan(ViewportRect(search, window).Bottom - 0.5),
            "the sort button shares the top row with the search box");
        Assert.That(ViewportRect(chips, window).Top, Is.GreaterThanOrEqualTo(ViewportRect(search, window).Bottom - 0.5),
            "only the chips drop to a second row when expanded");
        Assert.That(chips.Opacity, Is.EqualTo(1));
        window.Close();
    }

    [Test]
    public async Task BasicMode_OnAWideWindow_KeepsTheBasicPanelOnASingleRow()
    {
        var vm = await LoadedBasicModeVm();
        var (view, window) = ShowAt(vm, 1200);

        var search = Bar(view).FindControl<TextBox>("ItemsSearchBox")!;
        var trailing = Bar(view).FindControl<Control>("TrailingControls")!;

        Assert.That(ViewportRect(trailing, window).Left, Is.GreaterThan(ViewportRect(search, window).Right - 0.5),
            "when wide, the sort + advanced controls stay to the right of the items search box on one row");
        Assert.That(ViewportRect(trailing, window).Right, Is.LessThanOrEqualTo(window.Width + 0.5));
        window.Close();
    }

    [Test]
    public async Task SwitchToAdvanced_PrefillsTheSearchBoxWithTheSerializedQuery()
    {
        var vm = await LoadedBasicModeVm();
        var (view, _) = Show(vm);

        vm.SearchBar.SwitchToAdvancedCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.That(Bar(view).FindControl<TextBox>("SearchBox")!.Text, Is.EqualTo("collection = Trains"));
    }

    [Test]
    public async Task SearchBox_ExistsAndIsBoundToTheQueryText()
    {
        var vm = await LoadedVmWithOneColumn();

        var view = new PresetDetailView { DataContext = vm };
        var window = new Window { Content = view, Width = 900, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var box = Bar(view).FindControl<TextBox>("SearchBox");
        Assert.That(box, Is.Not.Null, "the query box must exist above the item list");
        Assert.That(box!.Text, Is.EqualTo(vm.SearchBar.Query.QueryText));

        vm.SearchBar.Query.QueryText = "name ~ loco";
        Dispatcher.UIThread.RunJobs();
        Assert.That(box.Text, Is.EqualTo("name ~ loco"));
    }
}
