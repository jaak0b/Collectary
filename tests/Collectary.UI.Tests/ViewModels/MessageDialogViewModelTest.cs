using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class MessageDialogViewModelTest
{
    [Test]
    public void Construct_ExposesMessageAndTitle()
    {
        var sut = new MessageDialogViewModel("Something went wrong", "Oops");

        Assert.Multiple(() =>
        {
            Assert.That(sut.Message, Is.EqualTo("Something went wrong"));
            Assert.That(sut.Title, Is.EqualTo("Oops"));
            Assert.That(sut.Completion.IsCompleted, Is.False);
        });
    }

    [Test]
    public async Task Ok_CompletesWithNull()
    {
        var sut = new MessageDialogViewModel("msg", "title");

        sut.OkCommand.Execute(null);
        var result = await sut.Completion;

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Ok_RaisesClosed()
    {
        var sut = new MessageDialogViewModel("msg", "title");
        var raised = false;
        sut.Closed += _ => raised = true;

        sut.OkCommand.Execute(null);

        Assert.That(raised, Is.True);
    }
}
