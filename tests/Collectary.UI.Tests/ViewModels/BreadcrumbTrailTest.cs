using FakeItEasy;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class BreadcrumbTrailTest
{
    private static BreadcrumbNode Node(string title) =>
        new(title, A.Fake<ViewModelBase>());

    [Test]
    public void WhenWithinLimit_ShowsAllAndCollapsesNothing()
    {
        var nodes = new[] { Node("A"), Node("B") };

        var trail = new BreadcrumbTrail<BreadcrumbNode>(nodes, maxVisible: 3);

        Assert.That(trail.Visible.Select(n => n.Title), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(trail.Collapsed, Is.Empty);
        Assert.That(trail.HasCollapsed, Is.False);
    }

    [Test]
    public void WhenOverLimit_KeepsTrailingVisibleAndCollapsesLeading()
    {
        var nodes = new[] { Node("A"), Node("B"), Node("C"), Node("D"), Node("E") };

        var trail = new BreadcrumbTrail<BreadcrumbNode>(nodes, maxVisible: 2);

        Assert.That(trail.Visible.Select(n => n.Title), Is.EqualTo(new[] { "D", "E" }));
        Assert.That(trail.Collapsed.Select(n => n.Title), Is.EqualTo(new[] { "A", "B", "C" }));
        Assert.That(trail.HasCollapsed, Is.True);
    }

    [Test]
    public void WhenMaxVisibleIsOne_ShowsOnlyCurrent()
    {
        var nodes = new[] { Node("A"), Node("B"), Node("C") };

        var trail = new BreadcrumbTrail<BreadcrumbNode>(nodes, maxVisible: 1);

        Assert.That(trail.Visible.Select(n => n.Title), Is.EqualTo(new[] { "C" }));
        Assert.That(trail.Collapsed.Select(n => n.Title), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void WhenMaxVisibleBelowOne_ClampsToOne()
    {
        var nodes = new[] { Node("A"), Node("B") };

        var trail = new BreadcrumbTrail<BreadcrumbNode>(nodes, maxVisible: 0);

        Assert.That(trail.Visible.Select(n => n.Title), Is.EqualTo(new[] { "B" }));
        Assert.That(trail.Collapsed.Select(n => n.Title), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public void WhenEmpty_ShowsNothing()
    {
        var trail = new BreadcrumbTrail<BreadcrumbNode>(System.Array.Empty<BreadcrumbNode>(), maxVisible: 3);

        Assert.That(trail.Visible, Is.Empty);
        Assert.That(trail.Collapsed, Is.Empty);
        Assert.That(trail.HasCollapsed, Is.False);
    }
}
