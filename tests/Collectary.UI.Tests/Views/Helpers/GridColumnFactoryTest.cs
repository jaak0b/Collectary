using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Collectary.UI.Views.Helpers;

namespace Collectary.UI.Tests.Views.Helpers;

[TestFixture]
public class GridColumnFactoryTest
{
    private sealed class Row;

    [Test]
    public void ActionColumn_ReservesRightGutter_SoTheScrollbarDoesNotOverlapTheMenuButton()
    {
        var actions = new (string Header, Action<Row> Run)[] { ("Edit", _ => { }) };
        var column = (DataGridTemplateColumn)GridColumnFactory.ActionColumn(actions);

        var cell = (StackPanel)column.CellTemplate!.Build(new Row())!;

        Assert.Multiple(() =>
        {
            Assert.That(cell.HorizontalAlignment, Is.EqualTo(HorizontalAlignment.Right));
            Assert.That(cell.Margin.Right, Is.GreaterThanOrEqualTo(16),
                "the ⋯ button needs a right gutter so the overlay scrollbar can't cover it");
        });
    }
}
