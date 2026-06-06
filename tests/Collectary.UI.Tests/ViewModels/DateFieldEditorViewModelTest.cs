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
}
