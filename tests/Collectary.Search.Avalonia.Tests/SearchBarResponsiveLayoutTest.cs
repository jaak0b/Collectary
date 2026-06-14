using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Search;
using Collectary.Search.Avalonia.Controls;
using Collectary.Search.ViewModels;

namespace Collectary.Search.Avalonia.Tests;

[TestFixture]
public class SearchBarResponsiveLayoutTest
{
    private readonly ILocalizationProvider _loc = new KeyLocalization();
    private const double NoRoom = 1;
    private const double AmpleRoom = 4000;

    private async Task<SearchBarViewModel> MakeBar(bool basicPreference, string query)
    {
        var item = new ItemQueryViewModel(
            new FakeRunner(), new FakeCatalog(), new QuerySuggestionEngine(new QueryLexer()), _loc,
            _ => Task.CompletedTask);
        var basic = new BasicFilterViewModel(
            new FakeCatalog(), _loc, _ => Task.CompletedTask, debounceMilliseconds: 0);
        var bar = new SearchBarViewModel(item, basic, _loc, () => basicPreference, _ => { });
        await basic.LoadAsync();
        await bar.InitializeAsync(query);
        return bar;
    }

    private SearchBar Show(SearchBarViewModel bar)
    {
        var control = new SearchBar { DataContext = bar };
        var window = new Window { Content = control, Width = AmpleRoom, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return control;
    }

    private DockPanel Basic(SearchBar control) => control.FindControl<DockPanel>("BasicPanel")!;

    private DockPanel Advanced(SearchBar control) => control.FindControl<DockPanel>("AdvancedPanel")!;

    [Test]
    public async Task ApplyResponsiveLayout_WithAmpleRoom_KeepsBasicPanelOnOneRow()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"));

        control.ApplyResponsiveLayout(AmpleRoom);

        Assert.That(Basic(control).Classes.Contains("narrow"), Is.False);
    }

    [Test]
    public async Task ApplyResponsiveLayout_WithNoRoom_StacksBasicPanel()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"));

        control.ApplyResponsiveLayout(NoRoom);

        Assert.That(Basic(control).Classes.Contains("narrow"), Is.True);
    }

    [Test]
    public async Task ApplyResponsiveLayout_WithNoRoom_CollapsesFiltersByDefault()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"));

        control.ApplyResponsiveLayout(NoRoom);

        Assert.That(Basic(control).Classes.Contains("filters-collapsed"), Is.True);
    }

    [Test]
    public async Task ApplyResponsiveLayout_WithAmpleRoom_DoesNotCollapseFilters()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"));

        control.ApplyResponsiveLayout(AmpleRoom);

        Assert.That(Basic(control).Classes.Contains("filters-collapsed"), Is.False);
    }

    [Test]
    public async Task ExpandingFilters_WhileNarrow_RemovesTheCollapsedClass()
    {
        var bar = await MakeBar(basicPreference: true, "Status = open");
        var control = Show(bar);
        control.ApplyResponsiveLayout(NoRoom);
        Assert.That(Basic(control).Classes.Contains("filters-collapsed"), Is.True);

        bar.IsFilterPanelExpanded = true;

        Assert.That(Basic(control).Classes.Contains("filters-collapsed"), Is.False);
    }

    [Test]
    public async Task ApplyResponsiveLayout_WithNoRoom_StacksAdvancedPanel()
    {
        var control = Show(await MakeBar(basicPreference: false, "Status = open"));

        control.ApplyResponsiveLayout(NoRoom);

        Assert.That(Advanced(control).Classes.Contains("narrow"), Is.True);
    }
}
