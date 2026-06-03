using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DurationFieldEditorViewModelTest
{
    [Test]
    public void SplitsLoadedMinutesIntoHoursAndMinutes()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue { TotalMinutes = 135 });
        Assert.That(sut.Hours, Is.EqualTo(2));
        Assert.That(sut.Minutes, Is.EqualTo(15));
        Assert.That(sut.HasValue, Is.True);
    }

    [Test]
    public void NullLoadHasNoValue()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue());
        Assert.That(sut.Hours, Is.Null);
        Assert.That(sut.Minutes, Is.Null);
        Assert.That(sut.HasValue, Is.False);
    }

    [Test]
    public void PersistsCombinedMinutes()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue())
        {
            Hours = 1,
            Minutes = 30
        };
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.EqualTo(90));
    }

    [Test]
    public void PersistsNullWhenBothZeroOrEmpty()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue());
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.Null);

        sut.Hours = 0;
        sut.Minutes = 0;
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.Null);
    }

    [Test]
    public void TreatsMissingComponentAsZero()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue()) { Hours = 2 };
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.EqualTo(120));
    }
}
