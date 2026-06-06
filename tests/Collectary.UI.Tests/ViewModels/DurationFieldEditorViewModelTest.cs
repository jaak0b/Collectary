using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DurationFieldEditorViewModelTest
{
    [Test]
    public void LoadsHoursAndMinutesAsText()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue { TotalMinutes = 135 });
        Assert.That(sut.Text, Is.EqualTo("2h 15m"));
        Assert.That(sut.HasError, Is.False);
    }

    [Test]
    public void LoadsWholeHoursWithoutMinutes()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue { TotalMinutes = 120 });
        Assert.That(sut.Text, Is.EqualTo("2h"));
    }

    [Test]
    public void LoadsMinutesOnly()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue { TotalMinutes = 45 });
        Assert.That(sut.Text, Is.EqualTo("45m"));
    }

    [Test]
    public void NullLoadsEmptyText()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue());
        Assert.That(sut.Text, Is.Empty);
        Assert.That(sut.HasError, Is.False);
    }

    [Test]
    public void ParsesHourMinuteText()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue()) { Text = "2h 30m" };
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.EqualTo(150));
    }

    [Test]
    public void ParsesColonText()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue()) { Text = "2:30" };
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.EqualTo(150));
    }

    [Test]
    public void ParsesPlainMinutes()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue()) { Text = "90" };
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.EqualTo(90));
    }

    [Test]
    public void ZeroPersistsNullWithoutError()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue()) { Text = "0" };
        Assert.That(sut.HasError, Is.False);
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.Null);
    }

    [Test]
    public void InvalidTextFlagsErrorAndPersistsNull()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue()) { Text = "abc" };
        Assert.That(sut.HasError, Is.True);
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.Null);
    }

    [Test]
    public void EmptyTextPersistsNull()
    {
        var sut = new DurationFieldEditorViewModel(new DurationFieldDefinition(), new DurationFieldValue { TotalMinutes = 60 }) { Text = "" };
        Assert.That(((DurationFieldValue)sut.GetCurrentValue()).TotalMinutes, Is.Null);
    }
}
