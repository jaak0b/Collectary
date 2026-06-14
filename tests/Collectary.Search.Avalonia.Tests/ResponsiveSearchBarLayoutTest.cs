using Collectary.Search.ViewModels;

namespace Collectary.Search.Avalonia.Tests;

[TestFixture]
public class ResponsiveSearchBarLayoutTest
{
    private readonly ResponsiveSearchBarLayout _layout = new();

    [Test]
    public void ShouldStack_ContentFitsComfortably_StaysWide()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 1000, naturalRowWidth: 690), Is.False);
    }

    [Test]
    public void ShouldStack_ContentWiderThanAvailable_Stacks()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 500, naturalRowWidth: 690), Is.True);
    }

    [Test]
    public void ShouldStack_ContentExactlyFills_StaysWide()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 690, naturalRowWidth: 690), Is.False);
    }

    [Test]
    public void ShouldStack_ContentOnePixelWider_Stacks()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 689, naturalRowWidth: 690), Is.True);
    }

    [Test]
    public void ShouldStack_NotYetMeasuredZeroWidth_StaysWide()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 0, naturalRowWidth: 690), Is.False);
    }
}
