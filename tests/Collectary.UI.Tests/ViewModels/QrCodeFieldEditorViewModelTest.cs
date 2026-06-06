using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class QrCodeFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(out List<string> generated)
    {
        var calls = new List<string>();
        generated = calls;
        var ctx = new ItemEditingContext(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        ctx.GenerateQrBitmap = content => { calls.Add(content); return null; };
        return ctx;
    }

    [Test]
    public void LoadsAndPersistsContent()
    {
        var ctx = MakeContext(out _);
        var sut = new QrCodeFieldEditorViewModel(new QrCodeFieldDefinition(),
            new QrCodeFieldValue { Content = "SHELF-A1" }, ctx);

        Assert.That(sut.Content, Is.EqualTo("SHELF-A1"));

        sut.Content = "BOX-42";
        Assert.That(((QrCodeFieldValue)sut.GetCurrentValue()).Content, Is.EqualTo("BOX-42"));
    }

    [Test]
    public void ChangingContent_RegeneratesPreview()
    {
        var ctx = MakeContext(out var generated);
        var sut = new QrCodeFieldEditorViewModel(new QrCodeFieldDefinition(), new QrCodeFieldValue(), ctx);

        sut.Content = "https://collectary.app/i/7";

        Assert.That(generated, Does.Contain("https://collectary.app/i/7"));
    }

    [Test]
    public void Content_LongerThan500_IsTruncatedTo500()
    {
        var ctx = MakeContext(out _);
        var sut = new QrCodeFieldEditorViewModel(new QrCodeFieldDefinition(), new QrCodeFieldValue(), ctx);

        sut.Content = new string('A', 750);

        Assert.That(sut.Content, Has.Length.EqualTo(500));
    }

    [Test]
    public void Content_At500_IsNotTruncated()
    {
        var ctx = MakeContext(out _);
        var sut = new QrCodeFieldEditorViewModel(new QrCodeFieldDefinition(), new QrCodeFieldValue(), ctx);

        var exact = new string('B', 500);
        sut.Content = exact;

        Assert.That(sut.Content, Is.EqualTo(exact));
    }

    [Test]
    public void Generation_ThatThrows_DoesNotCrashAndLeavesNoPreview()
    {
        var ctx = MakeContext(out _);
        ctx.GenerateQrBitmap = _ => throw new InvalidOperationException("Data too big");
        var sut = new QrCodeFieldEditorViewModel(new QrCodeFieldDefinition(), new QrCodeFieldValue(), ctx);

        Assert.That(() => sut.Content = "anything", Throws.Nothing);
        Assert.That(sut.Preview, Is.Null);
        Assert.That(sut.HasPreview, Is.False);
    }

    [Test]
    public void EmptyContent_ProducesNoPreview()
    {
        var ctx = MakeContext(out var generated);
        var sut = new QrCodeFieldEditorViewModel(new QrCodeFieldDefinition(), new QrCodeFieldValue { Content = "x" }, ctx);

        sut.Content = "   ";

        Assert.That(sut.Preview, Is.Null);
        Assert.That(sut.HasPreview, Is.False);
        Assert.That(generated, Does.Not.Contain("   "));
    }
}
