using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels.ListCells;

[TestFixture]
public class RatingListCellViewModelTest
{
    private static int[] LitPositions(RatingListCellViewModel cell) =>
        cell.Stars.Where(s => s.IsLit).Select(s => s.Position).ToArray();

    [Test]
    public void Stars_LightUpToValue()
    {
        var cell = new RatingListCellViewModel(new RatingFieldValue { Stars = 3 }, new RatingFieldDefinition { MaxStars = 5 });

        Assert.That(cell.Stars.Select(s => s.Position), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        Assert.That(LitPositions(cell), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Null_LeavesAllUnlit()
    {
        var cell = new RatingListCellViewModel(new RatingFieldValue { Stars = null }, new RatingFieldDefinition { MaxStars = 5 });

        Assert.That(LitPositions(cell), Is.Empty);
        Assert.That(cell.Stars, Has.Count.EqualTo(5));
    }

    [Test]
    public void RespectsMaxStars()
    {
        var cell = new RatingListCellViewModel(new RatingFieldValue { Stars = 2 }, new RatingFieldDefinition { MaxStars = 3 });

        Assert.That(cell.Stars, Has.Count.EqualTo(3));
        Assert.That(LitPositions(cell), Is.EqualTo(new[] { 1, 2 }));
    }
}
