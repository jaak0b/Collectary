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
    public void InlineGrid_ActionColumn_IsFirstAndFrozen()
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
            Assert.That(grid.FrozenColumnCount, Is.EqualTo(1), "the action column must be frozen so it never scrolls off screen");
            Assert.That(grid.Columns, Has.Count.GreaterThan(1));
            Assert.That(grid.Columns[0].Header, Is.EqualTo(""), "the action (⋯) column must be the first, frozen column");
        });
    }
}
