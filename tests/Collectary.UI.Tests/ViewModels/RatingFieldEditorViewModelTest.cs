using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class RatingFieldEditorViewModelTest
{
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
}
