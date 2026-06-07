using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.ViewModels;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class ListFieldEditorViewTest
{
    [Test]
    public void InlineGrid_ActionColumn_AppearsExactlyOnce_AsTheLastColumn()
    {
        var (registry, cellBuilder) = ListFieldEditorTestHarness.MakeFakes();
        var ctx = ListFieldEditorTestHarness.MakeContext(registry, cellBuilder);
        var definition = ListFieldEditorTestHarness.DefinitionWith();
        definition.InlineStyle = ListInlineStyle.Grid;
        var vm = new ListFieldEditorViewModel(definition, ListFieldEditorTestHarness.ValueWithEntries(1), ctx);

        var view = new ListFieldEditorView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        var grid = view.FindControl<DataGrid>("EntryGrid")!;

        Assert.Multiple(() =>
        {
            Assert.That(grid.Columns.Count(c => c.Header as string == ""), Is.EqualTo(1), "the ⋯ action column must appear exactly once");
            Assert.That(grid.Columns[^1].Header, Is.EqualTo(""), "the ⋯ action column must be the last column");
        });
    }
}
