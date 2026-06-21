using System.Globalization;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels.ListCells;

[TestFixture]
public class DateListCellViewModelTest
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
    public void WithoutTime_ShowsDateOnly()
    {
        var cell = new DateListCellViewModel(
            new DateFieldValue { Value = new DateTime(2025, 7, 4, 14, 30, 0) },
            new DateFieldDefinition { WithTime = false });

        Assert.That(cell.Text, Is.EqualTo("7/4/2025"));
    }

    [Test]
    public void WithTime_ShowsDateAndTime()
    {
        var cell = new DateListCellViewModel(
            new DateFieldValue { Value = new DateTime(2025, 7, 4, 14, 30, 0) },
            new DateFieldDefinition { WithTime = true });

        Assert.That(cell.Text, Is.EqualTo("7/4/2025 2:30 PM"));
    }

    [Test]
    public void NonDateDefinition_FallsBackToDateOnly()
    {
        var cell = new DateListCellViewModel(
            new DateFieldValue { Value = new DateTime(2025, 7, 4, 14, 30, 0) },
            new TextFieldDefinition());

        Assert.That(cell.Text, Is.EqualTo("7/4/2025"));
    }

    [Test]
    public void Empty_WhenNull()
    {
        var cell = new DateListCellViewModel(new DateFieldValue { Value = null }, new DateFieldDefinition());

        Assert.That(cell.Text, Is.Empty);
    }
}
