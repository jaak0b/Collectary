using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels.ListCells;

[TestFixture]
public class DecimalListCellViewModelTest
{
    [Test]
    public void Display_FormatsToConfiguredDecimalPlaces()
    {
        var sut = new DecimalListCellViewModel(
            new DecimalFieldValue { Value = 1.5m },
            new DecimalFieldDefinition { DecimalPlaces = 3 });

        Assert.That(sut.Display, Is.EqualTo("1.500"));
    }

    [Test]
    public void Display_RoundsToConfiguredDecimalPlaces()
    {
        var sut = new DecimalListCellViewModel(
            new DecimalFieldValue { Value = 1.2399m },
            new DecimalFieldDefinition { DecimalPlaces = 2 });

        Assert.That(sut.Display, Is.EqualTo("1.24"));
    }

    [Test]
    public void Display_Empty_WhenValueNull()
    {
        var sut = new DecimalListCellViewModel(
            new DecimalFieldValue { Value = null },
            new DecimalFieldDefinition { DecimalPlaces = 2 });

        Assert.That(sut.Display, Is.Empty);
    }
}
