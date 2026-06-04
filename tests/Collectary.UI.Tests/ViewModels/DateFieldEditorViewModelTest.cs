using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DateFieldEditorViewModelTest
{
    [Test]
    public void LoadsAsUtcOffset_AndPersistsUtc()
    {
        var date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition(), new DateFieldValue { Value = date });
        Assert.That(sut.Date!.Value.UtcDateTime, Is.EqualTo(date));
        Assert.That(((DateFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(date));
    }

    [Test]
    public void NullRoundTrips()
    {
        var sut = new DateFieldEditorViewModel(new DateFieldDefinition(), new DateFieldValue { Value = null });
        Assert.That(sut.Date, Is.Null);
        Assert.That(((DateFieldValue)sut.GetCurrentValue()).Value, Is.Null);
    }
}
