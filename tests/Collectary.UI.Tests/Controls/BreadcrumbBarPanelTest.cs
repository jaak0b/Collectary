using Avalonia;
using Avalonia.Controls;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class BreadcrumbBarPanelTest
{
    private static Border Crumb(double width) => new() { Child = new Border { Width = width, Height = 30 } };

    private static Border Overflow(double width)
    {
        var b = new Border { Child = new Border { Width = width, Height = 30 } };
        BreadcrumbBarPanel.SetIsOverflow(b, true);
        return b;
    }

    private static BreadcrumbBarPanel Build(double available, params Control[] children)
    {
        var panel = new BreadcrumbBarPanel();
        foreach (var c in children) panel.Children.Add(c);
        panel.Measure(new Size(available, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, available, panel.DesiredSize.Height));
        return panel;
    }

    [Test]
    public void WideEnough_AllCrumbsArranged_OverflowHidden()
    {
        var home = Crumb(100);
        var overflow = Overflow(40);
        var a = Crumb(100);
        var b = Crumb(100);
        var current = Crumb(100);

        Build(600, home, overflow, a, b, current);

        Assert.That(home.Bounds.Width, Is.GreaterThan(0));
        Assert.That(a.Bounds.Width, Is.GreaterThan(0));
        Assert.That(b.Bounds.Width, Is.GreaterThan(0));
        Assert.That(current.Bounds.Width, Is.GreaterThan(0));
        Assert.That(overflow.Bounds.Width, Is.EqualTo(0));
    }

    [Test]
    public void TooNarrow_CollapsesLeadingMiddle_ShowsOverflow()
    {
        var home = Crumb(100);
        var overflow = Overflow(40);
        var a = Crumb(100);
        var b = Crumb(100);
        var current = Crumb(100);

        Build(350, home, overflow, a, b, current);

        Assert.That(home.Bounds.Width, Is.GreaterThan(0));
        Assert.That(a.Bounds.Width, Is.EqualTo(0), "leading middle crumb collapses");
        Assert.That(b.Bounds.Width, Is.GreaterThan(0));
        Assert.That(current.Bounds.Width, Is.GreaterThan(0));
        Assert.That(overflow.Bounds.Width, Is.GreaterThan(0), "overflow button shows");
    }

    [Test]
    public void VeryNarrow_KeepsOnlyHomeAndCurrent()
    {
        var home = Crumb(100);
        var overflow = Overflow(40);
        var a = Crumb(100);
        var b = Crumb(100);
        var c = Crumb(100);
        var current = Crumb(100);

        var panel = Build(260, home, overflow, a, b, c, current);

        Assert.That(home.Bounds.Width, Is.GreaterThan(0));
        Assert.That(current.Bounds.Width, Is.GreaterThan(0));
        Assert.That(a.Bounds.Width, Is.EqualTo(0));
        Assert.That(b.Bounds.Width, Is.EqualTo(0));
        Assert.That(c.Bounds.Width, Is.EqualTo(0));
        Assert.That(overflow.Bounds.Width, Is.GreaterThan(0));
        Assert.That(panel.CollapsedIndices, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void OverflowReservationProperty_Change_InvalidatesMeasure()
    {
        var panel = new BreadcrumbBarPanel { OverflowReservation = 40 };
        panel.Measure(new Size(400, double.PositiveInfinity));
        Assert.That(panel.IsMeasureValid, Is.True);

        panel.OverflowReservation = 80;

        Assert.That(panel.IsMeasureValid, Is.False);
    }

    [Test]
    public void MeasureAndArrange_DoNotInvalidateMeasure_NoLayoutLoop()
    {
        var home = Crumb(100);
        var overflow = Overflow(40);
        var a = Crumb(100);
        var b = Crumb(100);
        var current = Crumb(100);

        var panel = Build(350, home, overflow, a, b, current);

        Assert.That(panel.IsMeasureValid, Is.True);
    }
}
