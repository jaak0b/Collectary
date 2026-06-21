using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FileAttachmentFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(
        Func<Task<(string, string)?>>? pick = null,
        Func<string, string, Task>? open = null,
        Func<string, Task>? delete = null)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        if (pick is not null) ctx.PickAndStoreFileAsync = pick;
        if (open is not null) ctx.ExportFileAsync = open;
        if (delete is not null) ctx.DeleteFileAsync = delete;
        return ctx;
    }

    [Test]
    public void LoadsExistingFiles()
    {
        var value = new FileAttachmentFieldValue { Files = [new("k1", "manual.pdf"), new("k2", "warranty.pdf")] };
        var sut = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(), value, MakeContext());

        Assert.That(sut.Attachments.Select(a => a.FileName), Is.EqualTo(new[] { "manual.pdf", "warranty.pdf" }));
        Assert.That(sut.HasAttachments, Is.True);
    }

    [Test]
    public async Task AddFile_AppendsPickedAttachment()
    {
        var sut = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(),
            new FileAttachmentFieldValue(),
            MakeContext(pick: () => Task.FromResult<(string, string)?>(("k9", "receipt.pdf"))));

        await sut.AddFileCommand.ExecuteAsync(null);

        var persisted = (FileAttachmentFieldValue)sut.GetCurrentValue();
        Assert.That(persisted.Files, Has.Count.EqualTo(1));
        Assert.That(persisted.Files[0], Is.EqualTo(new FileAttachment("k9", "receipt.pdf")));
    }

    [Test]
    public async Task AddFile_WhenPickCancelled_AddsNothing()
    {
        var sut = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(),
            new FileAttachmentFieldValue(),
            MakeContext(pick: () => Task.FromResult<(string, string)?>(null)));

        await sut.AddFileCommand.ExecuteAsync(null);

        Assert.That(sut.Attachments, Is.Empty);
    }

    [Test]
    public async Task RemoveFile_DropsEntryAndDeletesBlob()
    {
        var deleted = new List<string>();
        var value = new FileAttachmentFieldValue { Files = [new("k1", "a.pdf"), new("k2", "b.pdf")] };
        var sut = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(), value,
            MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; }));

        await sut.RemoveFileCommand.ExecuteAsync(sut.Attachments[0]);

        Assert.That(deleted, Is.EqualTo(new[] { "k1" }));
        Assert.That(((FileAttachmentFieldValue)sut.GetCurrentValue()).Files.Select(f => f.FileName),
            Is.EqualTo(new[] { "b.pdf" }));
    }

    [Test]
    public async Task SaveFile_ExportsByKeyAndName()
    {
        (string, string)? exported = null;
        var value = new FileAttachmentFieldValue { Files = [new("k1", "a.pdf")] };
        var sut = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(), value,
            MakeContext(open: (k, n) => { exported = (k, n); return Task.CompletedTask; }));

        await sut.Attachments[0].SaveAsCommand.ExecuteAsync(null);

        Assert.That(exported, Is.EqualTo(("k1", "a.pdf")));
    }

    [Test]
    public void HasAttachments_IsFalse_WhenNoFiles()
    {
        var sut = new FileAttachmentFieldEditorViewModel(
            new FileAttachmentFieldDefinition(), new FileAttachmentFieldValue(), MakeContext());

        Assert.That(sut.HasAttachments, Is.False);
    }

    [Test]
    public async Task AddFile_RaisesHasAttachmentsChanged()
    {
        var sut = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(),
            new FileAttachmentFieldValue(),
            MakeContext(pick: () => Task.FromResult<(string, string)?>(("k9", "receipt.pdf"))));
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await sut.AddFileCommand.ExecuteAsync(null);

        Assert.That(raised, Does.Contain(nameof(FileAttachmentFieldEditorViewModel.HasAttachments)));
    }

    [Test]
    public async Task RemoveFile_ForeignEntry_DoesNotDeleteBlob()
    {
        var deleted = new List<string>();
        var value = new FileAttachmentFieldValue { Files = [new("k1", "a.pdf")] };
        var sut = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(), value,
            MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; }));
        var foreign = new FileAttachmentEntryViewModel("zz", "other.pdf", MakeContext(), _ => Task.CompletedTask);

        await sut.RemoveFileCommand.ExecuteAsync(foreign);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.Empty);
            Assert.That(sut.Attachments, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void GetCurrentValue_AfterEdit_ReflectsNewFileNameWithStableKey()
    {
        var value = new FileAttachmentFieldValue { Files = [new("k1", "a.pdf")] };
        var sut = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(), value, MakeContext());

        sut.Attachments[0].EditingName = "final";

        var persisted = (FileAttachmentFieldValue)sut.GetCurrentValue();
        Assert.That(persisted.Files.Single(), Is.EqualTo(new FileAttachment("k1", "final.pdf")));
    }
}
