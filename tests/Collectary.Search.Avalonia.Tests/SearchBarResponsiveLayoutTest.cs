using Avalonia;
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

    private SearchBar Show(SearchBarViewModel bar, double width)
    {
        var control = new SearchBar { DataContext = bar };
        var window = new Window { Content = control, Width = width, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return control;
    }

    private SearchRowPanel Basic(SearchBar control) => control.FindControl<SearchRowPanel>("BasicPanel")!;

    private DockPanel Advanced(SearchBar control) => control.FindControl<DockPanel>("AdvancedPanel")!;

    private Control Named(SearchBar control, string name) => control.FindControl<Control>(name)!;

    [Test]
    public async Task BasicPanel_WithAmpleRoom_KeepsEverythingOnOneRow()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"), AmpleRoom);

        Assert.That(Basic(control).IsStacked, Is.False);
    }

    [Test]
    public async Task BasicPanel_WithNoRoom_Stacks()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"), NoRoom);

        Assert.That(Basic(control).IsStacked, Is.True);
    }

    [Test]
    public async Task BasicPanel_WhenCollapsed_HidesChipsButKeepsSortAndAdvanced()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"), NoRoom);

        Assert.Multiple(() =>
        {
            Assert.That(Named(control, "ChipArea").Opacity, Is.EqualTo(0));
            Assert.That(Named(control, "TrailingControls").Opacity, Is.EqualTo(1));
            Assert.That(Named(control, "SortButton").Opacity, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task BasicPanel_WithAmpleRoom_ShowsChipsAndTrailing()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"), AmpleRoom);

        Assert.Multiple(() =>
        {
            Assert.That(Named(control, "ChipArea").Bounds.Width, Is.GreaterThan(0));
            Assert.That(Named(control, "TrailingControls").Bounds.Width, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task ExpandingFilters_WhileNarrow_RevealsChips()
    {
        var bar = await MakeBar(basicPreference: true, "Status = open");
        var control = Show(bar, NoRoom);
        Assert.That(Named(control, "ChipArea").Opacity, Is.EqualTo(0));

        bar.IsFilterPanelExpanded = true;
        Dispatcher.UIThread.RunJobs();

        Assert.That(Named(control, "ChipArea").Opacity, Is.EqualTo(1));
    }

    [Test]
    public async Task ApplyResponsiveLayout_WithNoRoom_StacksAdvancedPanel()
    {
        var control = Show(await MakeBar(basicPreference: false, "Status = open"), AmpleRoom);

        control.ApplyResponsiveLayout(NoRoom);

        Assert.That(Advanced(control).Classes.Contains("narrow"), Is.True);
    }

    [Test]
    public async Task ApplyResponsiveLayout_WithAmpleRoom_KeepsAdvancedPanelWide()
    {
        var control = Show(await MakeBar(basicPreference: false, "Status = open"), AmpleRoom);

        control.ApplyResponsiveLayout(AmpleRoom);

        Assert.That(Advanced(control).Classes.Contains("narrow"), Is.False);
    }

    [Test]
    public async Task ApplyResponsiveLayout_FromBasicMode_StillStacksTheAdvancedPanelAtAModerateWidth()
    {
        var control = Show(await MakeBar(basicPreference: true, "Status = open"), AmpleRoom);

        control.ApplyResponsiveLayout(500);

        Assert.That(Advanced(control).Classes.Contains("narrow"), Is.True);
    }
}
