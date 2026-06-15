using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Collectary.Search.Avalonia.Controls;

namespace Collectary.Search.Avalonia.Tests;

[TestFixture]
public class SearchRowPanelTest
{
    private sealed record Row(SearchRowPanel Panel, Control Search, Control Chips, Control Trailing, Control Toggle);

    private const double SearchMin = 160;
    private const double ChipsWidth = 200;
    private const double TrailingWidth = 250;
    private const double ToggleWidth = 80;
    private const double Natural = SearchMin + 8 + ChipsWidth + 8 + TrailingWidth;

    private Row Build(bool expanded)
    {
        var search = new Border { MinWidth = SearchMin, Height = 30 };
        var chips = Fixed(ChipsWidth);
        var trailing = Fixed(TrailingWidth);
        var toggle = Fixed(ToggleWidth);
        var panel = new SearchRowPanel { FiltersExpanded = expanded };
        panel.Children.Add(search);
        panel.Children.Add(chips);
        panel.Children.Add(trailing);
        panel.Children.Add(toggle);
        return new Row(panel, search, chips, trailing, toggle);
    }

    private Border Fixed(double width) =>
        new() { Width = width, Height = 30, HorizontalAlignment = HorizontalAlignment.Left };

    private void Layout(SearchRowPanel panel, double width)
    {
        panel.Measure(new Size(width, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));
    }

    [Test]
    public void Layout_ContentFits_PutsEveryClusterOnOneRowAndHidesToggle()
    {
        var row = Build(expanded: false);

        Layout(row.Panel, width: 1000);

        Assert.Multiple(() =>
        {
            Assert.That(row.Panel.IsStacked, Is.False);
            Assert.That(row.Search.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Chips.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Trailing.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Search.Bounds.Width, Is.EqualTo(1000 - ChipsWidth - TrailingWidth - 16));
            Assert.That(row.Chips.Bounds.X, Is.GreaterThan(row.Search.Bounds.Right - 1));
            Assert.That(row.Trailing.Bounds.X, Is.GreaterThan(row.Chips.Bounds.Right - 1));
            Assert.That(row.Toggle.Opacity, Is.EqualTo(0));
            Assert.That(row.Toggle.IsHitTestVisible, Is.False);
        });
    }

    [Test]
    public void Layout_ContentDoesNotFitAndCollapsed_KeepsTrailingAndToggleOnTopRowAndHidesChips()
    {
        var row = Build(expanded: false);

        Layout(row.Panel, width: 560);

        Assert.Multiple(() =>
        {
            Assert.That(row.Panel.IsStacked, Is.True);
            Assert.That(row.Search.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Trailing.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Toggle.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Trailing.Opacity, Is.EqualTo(1));
            Assert.That(row.Toggle.Opacity, Is.EqualTo(1));
            Assert.That(row.Trailing.Bounds.X, Is.GreaterThan(row.Search.Bounds.Right - 1));
            Assert.That(row.Toggle.Bounds.X, Is.GreaterThan(row.Trailing.Bounds.Right - 1));
            Assert.That(row.Chips.Opacity, Is.EqualTo(0));
        });
    }

    [Test]
    public void Layout_ContentDoesNotFitAndExpanded_DropsOnlyChipsToASecondRow()
    {
        var row = Build(expanded: true);

        Layout(row.Panel, width: 560);

        Assert.Multiple(() =>
        {
            Assert.That(row.Panel.IsStacked, Is.True);
            Assert.That(row.Search.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Trailing.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Toggle.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Chips.Bounds.Y, Is.GreaterThan(row.Search.Bounds.Bottom - 1));
            Assert.That(row.Chips.Opacity, Is.EqualTo(1));
        });
    }

    [Test]
    public void Layout_TooNarrowForTheTopRowAndCollapsed_DropsTrailingControlsToTheirOwnRowBelowSearch()
    {
        var row = Build(expanded: false);

        Layout(row.Panel, width: 400);

        Assert.Multiple(() =>
        {
            Assert.That(row.Panel.IsStacked, Is.True);
            Assert.That(row.Search.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Search.Bounds.Width, Is.EqualTo(400), "search takes the whole first row");
            Assert.That(row.Trailing.Bounds.Y, Is.GreaterThan(row.Search.Bounds.Bottom - 1),
                "trailing controls move onto a second row instead of overlapping the search box");
            Assert.That(row.Toggle.Bounds.Y, Is.EqualTo(row.Trailing.Bounds.Y),
                "the filters toggle shares the controls row");
            Assert.That(row.Toggle.Bounds.X, Is.GreaterThan(row.Trailing.Bounds.Right - 1));
            Assert.That(row.Toggle.Bounds.Right, Is.LessThanOrEqualTo(400.5),
                "the controls row fits within the width, no clipping");
            Assert.That(row.Trailing.Opacity, Is.EqualTo(1));
            Assert.That(row.Toggle.Opacity, Is.EqualTo(1));
            Assert.That(row.Chips.Opacity, Is.EqualTo(0));
        });
    }

    [Test]
    public void Layout_TooNarrowForTheTopRowAndExpanded_PutsChipsOnAThirdRow()
    {
        var row = Build(expanded: true);

        Layout(row.Panel, width: 400);

        Assert.Multiple(() =>
        {
            Assert.That(row.Search.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Trailing.Bounds.Y, Is.GreaterThan(row.Search.Bounds.Bottom - 1));
            Assert.That(row.Chips.Bounds.Y, Is.GreaterThan(row.Trailing.Bounds.Bottom - 1),
                "chips fall below the trailing controls row");
            Assert.That(row.Chips.Opacity, Is.EqualTo(1));
        });
    }

    [Test]
    public void Layout_NarrowSharedTopRow_FitsWithoutClippingAndKeepsSearchAtItsMinimum()
    {
        var row = Build(expanded: false);

        Layout(row.Panel, width: 560);

        Assert.Multiple(() =>
        {
            Assert.That(row.Panel.IsStacked, Is.True);
            Assert.That(row.Trailing.Bounds.Y, Is.EqualTo(0),
                "with room for the search minimum plus the controls, they stay on the top row");
            Assert.That(row.Toggle.Bounds.Y, Is.EqualTo(0));
            Assert.That(row.Search.Bounds.Width, Is.GreaterThanOrEqualTo(SearchMin),
                "the search box never shrinks below its minimum on the shared top row");
            Assert.That(row.Toggle.Bounds.Right, Is.LessThanOrEqualTo(560.5),
                "the shared top row fits within the width, no clipping");
        });
    }

    [Test]
    public void Layout_ExactlyAtTheControlsBreakpoint_StaysOnTheSharedTopRowAtTheSearchMinimum()
    {
        var row = Build(expanded: false);
        double breakpoint = SearchMin + 8 + TrailingWidth + 8 + ToggleWidth;

        Layout(row.Panel, width: breakpoint);

        Assert.Multiple(() =>
        {
            Assert.That(row.Trailing.Bounds.Y, Is.EqualTo(0), "the controls still fit on the top row");
            Assert.That(row.Search.Bounds.Width, Is.GreaterThanOrEqualTo(SearchMin));
            Assert.That(row.Toggle.Bounds.Right, Is.LessThanOrEqualTo(breakpoint + 0.5));
        });
    }

    [Test]
    public void Layout_MeasuredUnconstrainedThenArrangedNarrow_StillStacksTheControls()
    {
        var row = Build(expanded: false);

        row.Panel.Measure(Size.Infinity);
        row.Panel.Arrange(new Rect(0, 0, 400, row.Panel.DesiredSize.Height));

        Assert.That(row.Trailing.Bounds.Y, Is.GreaterThan(row.Search.Bounds.Bottom - 1),
            "an unconstrained measure must not leave the controls-row decision keyed off a stale width");
    }

    [Test]
    public void Layout_AtTheExactNaturalWidth_StaysOnOneRow()
    {
        var row = Build(expanded: false);

        Layout(row.Panel, width: Natural);

        Assert.That(row.Panel.IsStacked, Is.False);
    }

    [Test]
    public void MeasureAndArrange_DoNotInvalidateMeasure_NoLayoutLoop()
    {
        var row = Build(expanded: true);

        Layout(row.Panel, width: 400);

        Assert.That(row.Panel.IsMeasureValid, Is.True);
    }
}
