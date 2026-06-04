using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class TimeFieldEditorViewModelTest
{
    [Test]
    public void ParsesLoadedHhMm()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue { Value = "09:05" });
        Assert.That(sut.Hour, Is.EqualTo(9));
        Assert.That(sut.Minute, Is.EqualTo(5));
    }

    [Test]
    public void IgnoresUnparseableValue()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue { Value = "not-a-time" });
        Assert.That(sut.Hour, Is.Null);
        Assert.That(sut.Minute, Is.Null);
    }

    [Test]
    public void PersistsZeroPaddedHhMm()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue()) { Hour = 7, Minute = 3 };
        Assert.That(((TimeFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo("07:03"));
    }

    [Test]
    public void PersistsNullWhenBothNull()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue());
        Assert.That(((TimeFieldValue)sut.GetCurrentValue()).Value, Is.Null);
    }

    [Test]
    public void TreatsMissingComponentAsZero()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue()) { Minute = 30 };
        Assert.That(((TimeFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo("00:30"));
    }
}
