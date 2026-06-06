using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DurationFieldEditorViewModelExtraTest
{
    private static DurationFieldEditorViewModel Make() =>
        new(new DurationFieldDefinition(), new DurationFieldValue());

    [Test]
    public void WhitespaceText_IsNotError()
    {
        var sut = Make();
        sut.Text = "   ";

        Assert.That(sut.HasError, Is.False);
    }

    [Test]
    public void GarbageText_IsError()
    {
        var sut = Make();
        sut.Text = "later";

        Assert.That(sut.HasError, Is.True);
    }

    [Test]
    public void TextChange_RaisesHasErrorNotification()
    {
        var sut = Make();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Text = "2h";

        Assert.That(raised, Does.Contain(nameof(sut.HasError)));
    }
}
