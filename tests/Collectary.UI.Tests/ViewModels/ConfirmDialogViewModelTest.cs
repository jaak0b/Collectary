using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ConfirmDialogViewModelTest
{
    private ConfirmDialogViewModel Build() =>
        new("Delete \"Widget\"?", confirmLabel: "Delete", cancelLabel: "Cancel", title: "Confirm");

    [Test]
    public void Construct_ExposesLabels()
    {
        var sut = Build();

        Assert.Multiple(() =>
        {
            Assert.That(sut.Message, Is.EqualTo("Delete \"Widget\"?"));
            Assert.That(sut.ConfirmLabel, Is.EqualTo("Delete"));
            Assert.That(sut.CancelLabel, Is.EqualTo("Cancel"));
            Assert.That(sut.Title, Is.EqualTo("Confirm"));
        });
    }

    [Test]
    public async Task Confirm_CompletesWithTrue()
    {
        var sut = Build();

        sut.ConfirmCommand.Execute(null);

        Assert.That(await sut.Completion, Is.EqualTo(true));
    }

    [Test]
    public async Task Cancel_CompletesWithFalse()
    {
        var sut = Build();

        sut.CancelCommand.Execute(null);

        Assert.That(await sut.Completion, Is.EqualTo(false));
    }
}
