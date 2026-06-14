using Collectary.Search.ViewModels;

namespace Collectary.Search.Avalonia.Tests;

[TestFixture]
public class ResponsiveSearchBarLayoutTest
{
    private readonly ResponsiveSearchBarLayout _layout = new(spacing: 24);

    [Test]
    public void ShouldStack_RowFitsWithRoomToSpare_StaysWide()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 1000, naturalRowWidth: 700), Is.False);
    }

    [Test]
    public void ShouldStack_RowWiderThanAvailable_Stacks()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 650, naturalRowWidth: 700), Is.True);
    }

    [Test]
    public void ShouldStack_RowFitsButWithinTheSpacingBuffer_StacksEarly()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 710, naturalRowWidth: 700), Is.True);
    }

    [Test]
    public void ShouldStack_RowPlusSpacingExactlyEqualsAvailable_StaysWide()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 724, naturalRowWidth: 700), Is.False);
    }

    [Test]
    public void ShouldStack_NotYetMeasuredZeroWidth_StaysWide()
    {
        Assert.That(_layout.ShouldStack(availableWidth: 0, naturalRowWidth: 700), Is.False);
    }
}
