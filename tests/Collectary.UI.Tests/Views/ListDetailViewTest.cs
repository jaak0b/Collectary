using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.ViewModels;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class ListDetailViewTest
{
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
}
