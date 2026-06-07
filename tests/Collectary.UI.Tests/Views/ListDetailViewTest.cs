using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.ViewModels;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class ListDetailViewTest
{
    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [Test]
    public void ActionColumn_AppearsExactlyOnce_AsTheLastColumn()
    {
        var (registry, cellBuilder) = ListFieldEditorTestHarness.MakeFakes();
        var ctx = ListFieldEditorTestHarness.MakeContext(registry, cellBuilder);
        var list = new ListFieldEditorViewModel(
            ListFieldEditorTestHarness.DefinitionWith(), ListFieldEditorTestHarness.ValueWithEntries(1), ctx);
        var vm = new ListDetailViewModel(list, ctx);

        var view = new ListDetailView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        var grid = view.FindControl<DataGrid>("EntryGrid")!;

        Assert.Multiple(() =>
        {
            Assert.That(grid.Columns.Count(c => c.Header as string == ""), Is.EqualTo(1), "the ⋯ action column must appear exactly once");
            Assert.That(grid.Columns[^1].Header, Is.EqualTo(""), "the ⋯ action column must be the last column");
        });
    }

    [Test]
    public void LanguageChange_RebuildsColumns_WithoutDuplicatingTheActionColumn()
    {
        var (registry, cellBuilder) = ListFieldEditorTestHarness.MakeFakes();
        var ctx = ListFieldEditorTestHarness.MakeContext(registry, cellBuilder);
        var list = new ListFieldEditorViewModel(
            ListFieldEditorTestHarness.DefinitionWith(), ListFieldEditorTestHarness.ValueWithEntries(1), ctx);
        var vm = new ListDetailViewModel(list, ctx);

        var view = new ListDetailView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        LocalizationService.Instance.Apply("de");
        Dispatcher.UIThread.RunJobs();

        var grid = view.FindControl<DataGrid>("EntryGrid")!;

        Assert.Multiple(() =>
        {
            Assert.That(grid.Columns.Count(c => c.Header as string == ""), Is.EqualTo(1),
                "rebuilding on a language change must not duplicate the action column");
            Assert.That(grid.Columns[^1].Header, Is.EqualTo(""));
        });
    }
}
