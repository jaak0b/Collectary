using Collectary.Presentation.Layout;

namespace Collectary.UI.Tests.Layout;

[TestFixture]
public class BreadcrumbCollapseEngineTest
{
    private BreadcrumbCollapseEngine _engine = null!;

    [SetUp]
    public void SetUp() => _engine = new BreadcrumbCollapseEngine();

    [Test]
    public void Resolve_Empty_ShowsNothing()
    {
        var result = _engine.Resolve(Array.Empty<double>(), availableWidth: 500, overflowWidth: 40, homeIndex: 0, currentIndex: 0);

        Assert.That(result.VisibleIndices, Is.Empty);
        Assert.That(result.CollapsedIndices, Is.Empty);
        Assert.That(result.ShowOverflow, Is.False);
        Assert.That(result.MustTrimCurrent, Is.False);
    }

    [Test]
    public void Resolve_SingleItemThatFits_VisibleNoTrim()
    {
        var result = _engine.Resolve(new[] { 120.0 }, availableWidth: 500, overflowWidth: 40, homeIndex: 0, currentIndex: 0);

        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 0 }));
        Assert.That(result.ShowOverflow, Is.False);
        Assert.That(result.MustTrimCurrent, Is.False);
    }

    [Test]
    public void Resolve_SingleItemTooWide_VisibleAndTrims()
    {
        var result = _engine.Resolve(new[] { 600.0 }, availableWidth: 500, overflowWidth: 40, homeIndex: 0, currentIndex: 0);

        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 0 }));
        Assert.That(result.ShowOverflow, Is.False);
        Assert.That(result.MustTrimCurrent, Is.True);
    }

    [Test]
    public void Resolve_AllFit_EverythingVisibleNoOverflow()
    {
        var widths = new[] { 100.0, 100.0, 100.0, 100.0 };
        var result = _engine.Resolve(widths, availableWidth: 500, overflowWidth: 40, homeIndex: 0, currentIndex: 3);

        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(result.CollapsedIndices, Is.Empty);
        Assert.That(result.ShowOverflow, Is.False);
        Assert.That(result.MustTrimCurrent, Is.False);
    }

    [Test]
    public void Resolve_AllFitExactly_EverythingVisible()
    {
        var widths = new[] { 100.0, 100.0, 100.0, 100.0 };
        var result = _engine.Resolve(widths, availableWidth: 400, overflowWidth: 40, homeIndex: 0, currentIndex: 3);

        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(result.ShowOverflow, Is.False);
    }

    [Test]
    public void Resolve_JustOver_CollapsesExactlyOneLeadingMiddleItem()
    {
        var widths = new[] { 100.0, 100.0, 100.0, 100.0 };
        var result = _engine.Resolve(widths, availableWidth: 350, overflowWidth: 40, homeIndex: 0, currentIndex: 3);

        Assert.That(result.ShowOverflow, Is.True);
        Assert.That(result.CollapsedIndices, Is.EqualTo(new[] { 1 }));
        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 0, 2, 3 }));
        Assert.That(result.MustTrimCurrent, Is.False);
    }

    [Test]
    public void Resolve_Narrow_KeepsHomeAndCurrentWhenHomeStillFits()
    {
        var widths = new[] { 100.0, 100.0, 100.0, 100.0, 100.0 };
        var result = _engine.Resolve(widths, availableWidth: 260, overflowWidth: 40, homeIndex: 0, currentIndex: 4);

        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 0, 4 }));
        Assert.That(result.CollapsedIndices, Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(result.ShowOverflow, Is.True);
        Assert.That(result.MustTrimCurrent, Is.False);
    }

    [Test]
    public void Resolve_HomeHasPriorityOverMiddleItems()
    {
        var widths = new[] { 100.0, 300.0, 300.0, 100.0 };
        var result = _engine.Resolve(widths, availableWidth: 300, overflowWidth: 40, homeIndex: 0, currentIndex: 3);

        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 0, 3 }));
        Assert.That(result.CollapsedIndices, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(result.ShowOverflow, Is.True);
    }

    [Test]
    public void Resolve_VeryNarrow_DropsHomeIntoOverflow()
    {
        var widths = new[] { 120.0, 100.0, 100.0, 300.0 };
        var result = _engine.Resolve(widths, availableWidth: 380, overflowWidth: 40, homeIndex: 0, currentIndex: 3);

        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 3 }));
        Assert.That(result.CollapsedIndices, Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(result.ShowOverflow, Is.True);
    }

    [Test]
    public void Resolve_TwoItemsThatDoNotFit_DropsHomeKeepsCurrent()
    {
        var widths = new[] { 300.0, 300.0 };
        var result = _engine.Resolve(widths, availableWidth: 400, overflowWidth: 40, homeIndex: 0, currentIndex: 1);

        Assert.That(result.VisibleIndices, Is.EqualTo(new[] { 1 }));
        Assert.That(result.CollapsedIndices, Is.EqualTo(new[] { 0 }));
        Assert.That(result.ShowOverflow, Is.True);
    }
}
