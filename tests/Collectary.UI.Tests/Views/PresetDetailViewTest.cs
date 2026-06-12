using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.Search;
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

        vm.Query.Suggestions.Add(new QuerySuggestion("name", "name", 0, 0, QuerySuggestionKind.Field));
        vm.Query.AreSuggestionsOpen = true;
        Dispatcher.UIThread.RunJobs();

        var list = view.FindControl<ListBox>("SuggestionList")!;
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

    private static IReadOnlyList<Button> ChipButtons(PresetDetailView view) =>
        Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(view)
            .OfType<Button>().Where(b => b.Classes.Contains("chip")).ToList();

    [Test]
    public async Task BasicMode_ShowsTheChipBarAndHidesTheAdvancedBox()
    {
        var vm = await LoadedBasicModeVm();
        var (view, _) = Show(vm);

        var basic = view.FindControl<Control>("BasicPanel")!;
        var advanced = view.FindControl<Control>("AdvancedPanel")!;
        Assert.That(vm.IsBasicMode, Is.True);
        Assert.That(basic.IsVisible, Is.True);
        Assert.That(advanced.IsVisible, Is.False);

        vm.SwitchToAdvancedCommand.Execute(null);
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

        vm.BasicFilter.AddChipCommand.Execute("Status");
        Dispatcher.UIThread.RunJobs();
        Assert.That(ChipButtons(view), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task BasicMode_HasMoreAndSortControls()
    {
        var vm = await LoadedBasicModeVm();
        var (view, _) = Show(vm);

        Assert.That(view.FindControl<Button>("MoreButton"), Is.Not.Null);
        var sortBox = view.FindControl<ComboBox>("SortFieldBox")!;
        Assert.That(sortBox.ItemsSource, Is.SameAs(vm.BasicFilter.SortFieldOptions));
    }

    [Test]
    public async Task SwitchToAdvanced_PrefillsTheSearchBoxWithTheSerializedQuery()
    {
        var vm = await LoadedBasicModeVm();
        var (view, _) = Show(vm);

        vm.SwitchToAdvancedCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.That(view.FindControl<TextBox>("SearchBox")!.Text, Is.EqualTo("collection = Trains"));
    }

    [Test]
    public async Task SearchBox_ExistsAndIsBoundToTheQueryText()
    {
        var vm = await LoadedVmWithOneColumn();

        var view = new PresetDetailView { DataContext = vm };
        var window = new Window { Content = view, Width = 900, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var box = view.FindControl<TextBox>("SearchBox");
        Assert.That(box, Is.Not.Null, "the query box must exist above the item list");
        Assert.That(box!.Text, Is.EqualTo(vm.Query.QueryText));

        vm.Query.QueryText = "name ~ loco";
        Dispatcher.UIThread.RunJobs();
        Assert.That(box.Text, Is.EqualTo("name ~ loco"));
    }
}
