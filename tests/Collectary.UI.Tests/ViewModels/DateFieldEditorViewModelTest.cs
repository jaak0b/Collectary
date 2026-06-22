using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DateFieldEditorViewModelTest
{
    [Test]
    public void LoadsStoredDate_AndPersistsIt()
    {
        var date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition(), new DateFieldValue { Value = date });
        Assert.That(sut.Date, Is.EqualTo(date));
        Assert.That(((DateFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(date));
    }

    [Test]
    public void NullRoundTrips()
    {
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition(), new DateFieldValue { Value = null });
        Assert.That(sut.Date, Is.Null);
        Assert.That(((DateFieldValue)sut.GetCurrentValue()).Value, Is.Null);
    }

    [Test]
    public void GetCurrentValue_PersistsSelectedDateAsUtc()
    {
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition(), new DateFieldValue())
        {
            Date = new DateTime(2025, 7, 4)
        };

        var value = ((DateFieldValue)sut.GetCurrentValue()).Value;
        Assert.That(value, Is.EqualTo(new DateTime(2025, 7, 4)));
        Assert.That(value!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void WithTime_ComposesDateAndTime()
    {
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition { WithTime = true }, new DateFieldValue())
        {
            Date = new DateTime(2025, 7, 4),
            Time = new TimeSpan(14, 30, 0)
        };

        var value = ((DateFieldValue)sut.GetCurrentValue()).Value;
        Assert.That(value, Is.EqualTo(new DateTime(2025, 7, 4, 14, 30, 0)));
        Assert.That(value!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void WithoutTime_StoresDateOnly_EvenIfADateCarriesTime()
    {
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition { WithTime = false }, new DateFieldValue())
        {
            Date = new DateTime(2025, 7, 4, 9, 15, 0)
        };

        var value = ((DateFieldValue)sut.GetCurrentValue()).Value;
        Assert.That(value, Is.EqualTo(new DateTime(2025, 7, 4)));
    }

    [Test]
    public void Load_WithoutTime_LeavesTimeNull_EvenIfStoredValueHasATime()
    {
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition { WithTime = false },
            new DateFieldValue { Value = new DateTime(2025, 7, 4, 14, 30, 0) });

        Assert.That(sut.Time, Is.Null);
    }

    [Test]
    public void Load_WithTime_SplitsStoredValueIntoDateAndTime()
    {
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition { WithTime = true },
            new DateFieldValue { Value = new DateTime(2025, 7, 4, 14, 30, 0) });

        Assert.That(sut.Date!.Value.Date, Is.EqualTo(new DateTime(2025, 7, 4)));
        Assert.That(sut.Time, Is.EqualTo(new TimeSpan(14, 30, 0)));
    }
}
