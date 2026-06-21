using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class RatingFieldEditorViewModelTest
{
    private static int[] LitPositions(RatingFieldEditorViewModel sut) =>
        sut.StarItems.Where(s => s.IsLit).Select(s => s.Position).ToArray();

    [Test]
    public void LoadsZeroFromNull_AndPersistsNullForZero()
    {
        var def = new RatingFieldDefinition { MaxStars = 10 };
        var sut = new RatingFieldEditorViewModel(def, new RatingFieldValue { Stars = null });
        Assert.That(sut.Stars, Is.EqualTo(0));
        Assert.That(sut.MaxStars, Is.EqualTo(10));
        Assert.That(((RatingFieldValue)sut.GetCurrentValue()).Stars, Is.Null);

        sut.Stars = 4;
        Assert.That(((RatingFieldValue)sut.GetCurrentValue()).Stars, Is.EqualTo(4));
    }

    [Test]
    public void StarItems_HasOnePositionPerMaxStar()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition { MaxStars = 5 }, new RatingFieldValue());

        Assert.That(sut.StarItems.Select(s => s.Position), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Ctor_LightsExistingRating()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition { MaxStars = 5 },
            new RatingFieldValue { Stars = 3 });

        Assert.That(LitPositions(sut), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void SetRating_SetsStarsAndLightsUpToPosition()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition { MaxStars = 5 }, new RatingFieldValue());

        sut.SetRating(4);

        Assert.That(sut.Stars, Is.EqualTo(4));
        Assert.That(LitPositions(sut), Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void SetRating_OnCurrentValue_ClearsToUnset()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition { MaxStars = 5 },
            new RatingFieldValue { Stars = 3 });

        sut.SetRating(3);

        Assert.That(sut.Stars, Is.EqualTo(0));
        Assert.That(LitPositions(sut), Is.Empty);
        Assert.That(((RatingFieldValue)sut.GetCurrentValue()).Stars, Is.Null);
    }

    [Test]
    public void PreviewRating_LightsUpToPosition_WithoutChangingStars()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition { MaxStars = 5 },
            new RatingFieldValue { Stars = 2 });

        sut.PreviewRating(5);

        Assert.That(sut.Stars, Is.EqualTo(2));
        Assert.That(LitPositions(sut), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Randomize_SetsRating_EvenWhenRollEqualsCurrentValue()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition { MaxStars = 5 },
            new RatingFieldValue { Stars = 3 });
        var data = A.Fake<ISampleData>();
        A.CallTo(() => data.Int(1, 5)).Returns(3);

        sut.Randomize(data);

        Assert.That(sut.Stars, Is.EqualTo(3));
        Assert.That(LitPositions(sut), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ClearPreview_RevertsToCommittedStars()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition { MaxStars = 5 },
            new RatingFieldValue { Stars = 2 });
        sut.PreviewRating(5);

        sut.ClearPreview();

        Assert.That(LitPositions(sut), Is.EqualTo(new[] { 1, 2 }));
    }
}
