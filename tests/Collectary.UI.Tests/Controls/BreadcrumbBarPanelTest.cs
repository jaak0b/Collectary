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
        Assert.That(overflow.Opacity, Is.EqualTo(0));
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
        Assert.That(a.Opacity, Is.EqualTo(0), "leading middle crumb collapses");
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
        Assert.That(a.Opacity, Is.EqualTo(0));
        Assert.That(b.Opacity, Is.EqualTo(0));
        Assert.That(c.Opacity, Is.EqualTo(0));
        Assert.That(overflow.Bounds.Width, Is.GreaterThan(0));
        Assert.That(panel.CollapsedIndices, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void VeryNarrow_DropsHomeWhenItCannotFit()
    {
        var home = Crumb(120);
        var overflow = Overflow(40);
        var a = Crumb(100);
        var b = Crumb(100);
        var current = Crumb(300);

        var panel = Build(380, home, overflow, a, b, current);

        Assert.That(home.Opacity, Is.EqualTo(0), "home folds into the overflow when it cannot fit");
        Assert.That(current.Bounds.Width, Is.GreaterThan(0));
        Assert.That(overflow.Bounds.Width, Is.GreaterThan(0));
        Assert.That(panel.CollapsedIndices, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void CollapsedCrumbs_AreFullyTransparent_NotPaintedAtOrigin()
    {
        var home = Crumb(100);
        var overflow = Overflow(40);
        var a = Crumb(100);
        var b = Crumb(100);
        var current = Crumb(100);

        Build(350, home, overflow, a, b, current);

        Assert.That(a.Opacity, Is.EqualTo(0), "a collapsed crumb must be transparent so it never paints over the visible crumbs");
        Assert.That(a.IsHitTestVisible, Is.False, "a collapsed crumb must not steal clicks meant for the overflow button");
        Assert.That(home.Opacity, Is.EqualTo(1), "a visible crumb stays opaque");
        Assert.That(home.IsHitTestVisible, Is.True, "a visible crumb stays clickable");
    }

    [Test]
    public void HiddenOverflow_IsFullyTransparentAndNotClickable()
    {
        var home = Crumb(100);
        var overflow = Overflow(40);
        var a = Crumb(100);
        var current = Crumb(100);

        Build(600, home, overflow, a, current);

        Assert.That(overflow.Opacity, Is.EqualTo(0), "the hidden overflow button must be transparent");
        Assert.That(overflow.IsHitTestVisible, Is.False, "the hidden overflow button must not capture clicks");
    }

    [Test]
    public void LastVisibleCrumb_LeavesTrailingMargin_SoItDoesNotTouchTheEdge()
    {
        var home = Crumb(100);
        var overflow = Overflow(40);
        var a = Crumb(100);
        var current = Crumb(400);

        const double available = 300;
        Build(available, home, overflow, a, current);

        Assert.That(current.Bounds.Right, Is.LessThanOrEqualTo(available - 8),
            "the last crumb must stop short of the right edge so it never butts into the profile");
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
