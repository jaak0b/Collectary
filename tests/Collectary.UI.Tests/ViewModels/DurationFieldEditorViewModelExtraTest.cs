using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DurationFieldEditorViewModelExtraTest
{
    private static DurationFieldEditorViewModel Make() =>
        new(new DurationFieldDefinition(), new DurationFieldValue());

    [Test]
    public void HasValue_TrueWhenOnlyHoursSet()
    {
        var sut = Make();
        sut.Hours = 2;
        sut.Minutes = null;

        Assert.That(sut.HasValue, Is.True);
    }

    [Test]
    public void HasValue_TrueWhenOnlyMinutesSet()
    {
        var sut = Make();
        sut.Hours = null;
        sut.Minutes = 30;

        Assert.That(sut.HasValue, Is.True);
    }

    [Test]
    public void HasValue_FalseWhenBothNull()
    {
        var sut = Make();
        sut.Hours = null;
        sut.Minutes = null;

        Assert.That(sut.HasValue, Is.False);
    }

    [Test]
    public void HoursChange_RaisesHasValueNotification()
    {
        var sut = Make();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Hours = 1;

        Assert.That(raised, Does.Contain(nameof(sut.HasValue)));
    }
}
