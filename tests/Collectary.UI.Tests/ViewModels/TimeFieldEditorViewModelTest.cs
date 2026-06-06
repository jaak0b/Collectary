using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class TimeFieldEditorViewModelTest
{
    [Test]
    public void LoadsStoredValueIntoText()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue { Value = "14:30" });
        Assert.That(sut.Text, Is.EqualTo("14:30"));
        Assert.That(sut.HasError, Is.False);
    }

    [Test]
    public void NullValueLoadsEmptyTextWithoutError()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue());
        Assert.That(sut.Text, Is.Empty);
        Assert.That(sut.HasError, Is.False);
    }

    [Test]
    public void NormalizesAndPersistsTypedTime()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue()) { Text = "9:05" };
        Assert.That(sut.HasError, Is.False);
        Assert.That(((TimeFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo("09:05"));
    }

    [Test]
    public void EmptyTextPersistsNull()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue { Value = "08:00" }) { Text = "" };
        Assert.That(((TimeFieldValue)sut.GetCurrentValue()).Value, Is.Null);
    }

    [Test]
    public void InvalidTextFlagsErrorAndPersistsNull()
    {
        var sut = new TimeFieldEditorViewModel(new TimeFieldDefinition(), new TimeFieldValue()) { Text = "25:61" };
        Assert.That(sut.HasError, Is.True);
        Assert.That(((TimeFieldValue)sut.GetCurrentValue()).Value, Is.Null);
    }
}
