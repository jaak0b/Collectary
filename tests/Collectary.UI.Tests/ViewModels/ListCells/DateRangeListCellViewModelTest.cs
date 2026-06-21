using System.Globalization;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels.ListCells;

[TestFixture]
public class DateRangeListCellViewModelTest
{
    private CultureInfo _original = null!;

    [SetUp]
    public void PinCulture()
    {
        _original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
    }

    [TearDown]
    public void RestoreCulture() => CultureInfo.CurrentCulture = _original;

    [Test]
    public void Text_FormatsBothEnds_InCurrentCultureWithArrow()
    {
        var cell = new DateRangeListCellViewModel(
            new DateRangeFieldValue { From = new DateTime(2018, 5, 1), To = new DateTime(2020, 6, 30) },
            new DateRangeFieldDefinition());

        Assert.That(cell.Text, Is.EqualTo("5/1/2018 → 6/30/2020"));
    }

    [Test]
    public void Text_OpenEnd_ShowsPlaceholderForMissingSide()
    {
        var cell = new DateRangeListCellViewModel(
            new DateRangeFieldValue { From = new DateTime(2018, 5, 1), To = null },
            new DateRangeFieldDefinition());

        Assert.That(cell.Text, Is.EqualTo("5/1/2018 → …"));
    }

    [Test]
    public void Text_Empty_WhenBothNull()
    {
        var cell = new DateRangeListCellViewModel(new DateRangeFieldValue(), new DateRangeFieldDefinition());

        Assert.That(cell.Text, Is.Empty);
    }
}
