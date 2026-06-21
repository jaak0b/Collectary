using System.ComponentModel;
using FakeItEasy;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FileAttachmentEntryViewModelTest
{
    private static FileAttachmentEntryViewModel Entry(string key, string fileName) =>
        new(key, fileName, MakeContext(), _ => Task.CompletedTask);

    private static ItemEditingContext MakeContext() => new(
        editorRegistry: A.Fake<IFieldEditorRegistry>(),
        listCellBuilder: A.Fake<IListCellBuilder>(),
        goBack: () => { },
        pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
        exportImageAsync: (_, _) => Task.CompletedTask,
        loadImageBitmap: _ => null,
        deleteImageAsync: _ => Task.CompletedTask);

    [Test]
    public void Ctor_SeedsEditingNameWithBaseNameOnly()
    {
        var entry = Entry("k1", "manual.pdf");

        Assert.Multiple(() =>
        {
            Assert.That(entry.EditingName, Is.EqualTo("manual"));
            Assert.That(entry.Extension, Is.EqualTo(".pdf"));
            Assert.That(entry.FileName, Is.EqualTo("manual.pdf"));
        });
    }

    [Test]
    public void FileName_CombinesEditingNameAndExtension()
    {
        var entry = Entry("k1", "manual.pdf");
        entry.EditingName = "handbook";

        Assert.That(entry.FileName, Is.EqualTo("handbook.pdf"));
    }

    [Test]
    public void FileName_IgnoresUserTypedExtension()
    {
        var entry = Entry("k1", "manual.pdf");
        entry.EditingName = "x.txt";

        Assert.That(entry.FileName, Is.EqualTo("x.txt.pdf"));
    }

    [Test]
    public void FileName_WhenBlank_FallsBackToOriginalName()
    {
        var entry = Entry("k1", "manual.pdf");
        entry.EditingName = "   ";

        Assert.That(entry.FileName, Is.EqualTo("manual.pdf"));
    }

    [Test]
    public void FileName_ExtensionlessFile_EqualsEditingName()
    {
        var entry = Entry("k1", "README");
        entry.EditingName = "NOTES";

        Assert.Multiple(() =>
        {
            Assert.That(entry.Extension, Is.Empty);
            Assert.That(entry.FileName, Is.EqualTo("NOTES"));
        });
    }

    [Test]
    public void EditingName_Changed_RaisesFileNameNotification()
    {
        var entry = Entry("k1", "manual.pdf");
        var raised = new List<string?>();
        ((INotifyPropertyChanged)entry).PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        entry.EditingName = "renamed";

        Assert.That(raised, Does.Contain(nameof(FileAttachmentEntryViewModel.FileName)));
    }

    [Test]
    public async Task SaveAs_ExportsByKeyAndCurrentFileName()
    {
        (string, string)? exported = null;
        var ctx = MakeContext();
        ctx.ExportFileAsync = (k, n) => { exported = (k, n); return Task.CompletedTask; };
        var entry = new FileAttachmentEntryViewModel("k1", "manual.pdf", ctx, _ => Task.CompletedTask);
        entry.EditingName = "renamed";

        await entry.SaveAsCommand.ExecuteAsync(null);

        Assert.That(exported, Is.EqualTo(("k1", "renamed.pdf")));
    }

    [Test]
    public async Task Delete_InvokesRemoveCallback()
    {
        FileAttachmentEntryViewModel? removed = null;
        var entry = new FileAttachmentEntryViewModel("k1", "manual.pdf", MakeContext(),
            e => { removed = e; return Task.CompletedTask; });

        await entry.DeleteCommand.ExecuteAsync(null);

        Assert.That(removed, Is.SameAs(entry));
    }
}
