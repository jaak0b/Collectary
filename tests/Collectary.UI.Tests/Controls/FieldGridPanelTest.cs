using Avalonia;
using Avalonia.Controls;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class FieldGridPanelTest
{
    [Test]
    public void ColumnCountProperty_Change_InvalidatesMeasure()
    {
        var panel = new FieldGridPanel { ColumnCount = 1 };
        panel.Measure(new Size(400, double.PositiveInfinity));
        Assert.That(panel.IsMeasureValid, Is.True);

        panel.ColumnCount = 2;

        Assert.That(panel.IsMeasureValid, Is.False);
    }

    [Test]
    public void ArrangeOverride_TwoChildrenInTwoCols_PlacedSideByide()
    {
        var panel = new FieldGridPanel { ColumnCount = 2, MinColumnWidth = 100, ColumnSpacing = 0 };
        panel.Children.Add(new Border { Height = 40 });
        panel.Children.Add(new Border { Height = 40 });

        panel.Measure(new Size(200, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 200, panel.DesiredSize.Height));

        Assert.That(panel.Children[0].Bounds.Y, Is.EqualTo(0));
        Assert.That(panel.Children[1].Bounds.Y, Is.EqualTo(0));
        Assert.That(panel.Children[1].Bounds.X, Is.GreaterThan(0));
    }

    [Test]
    public void MeasureAndArrange_ConsistentWidth_ReusesCachedRows()
    {
        var panel = new FieldGridPanel { ColumnCount = 2, MinColumnWidth = 100, ColumnSpacing = 0 };
        panel.Children.Add(new Border { Height = 30 });
        panel.Children.Add(new Border { Height = 30 });

        panel.Measure(new Size(200, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 200, 30));

        Assert.That(panel.Children[0].Bounds.Y, Is.EqualTo(0));
        Assert.That(panel.Children[1].Bounds.Y, Is.EqualTo(0));
    }
}
