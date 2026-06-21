using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class MultiImageFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(Func<string, Task>? delete = null, Func<string, string, Task>? export = null)
        => new(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: export ?? ((_, _) => Task.CompletedTask),
            loadImageBitmap: _ => null,
            deleteImageAsync: delete ?? (_ => Task.CompletedTask));

    private static MultiImagePicture Pic(string key, string name) => new(key, name);

    [Test]
    public void LoadsExistingPicturesInOrder()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg"), Pic("c", "c.jpg")] }, MakeContext());

        Assert.That(sut.Images.Select(i => i.Key), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(sut.HasImages, Is.True);
    }

    [Test]
    public void GetCurrentValue_PersistsPictureOrderAndFileNames()
    {
        var value = new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg")] };
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(), value, MakeContext());

        var persisted = (MultiImageFieldValue)sut.GetCurrentValue();

        Assert.That(persisted.Pictures.Select(p => p.Key), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(persisted.Pictures.Select(p => p.FileName), Is.EqualTo(new[] { "a.jpg", "b.jpg" }));
    }

    [Test]
    public async Task RemoveImage_DropsEntryAndDeletesBlob()
    {
        var deleted = new List<string>();
        var value = new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg")] };
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(), value,
            MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; }));

        await sut.RemoveImageCommand.ExecuteAsync(sut.Images[0]);

        Assert.That(deleted, Is.EqualTo(new[] { "a" }));
        Assert.That(((MultiImageFieldValue)sut.GetCurrentValue()).Pictures.Select(p => p.Key), Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public async Task Entry_SaveAs_ExportsWithTheStoredFileName()
    {
        string? exportedKey = null;
        string? exportedName = null;
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue { Pictures = [Pic("abc-123_photo.jpg", "photo.jpg")] },
            MakeContext(export: (key, name) => { exportedKey = key; exportedName = name; return Task.CompletedTask; }));

        await sut.Images[0].SaveAsCommand.ExecuteAsync(null);

        Assert.That(exportedKey, Is.EqualTo("abc-123_photo.jpg"));
        Assert.That(exportedName, Is.EqualTo("photo.jpg"));
    }

    [Test]
    public async Task Entry_Delete_RemovesEntryAndDeletesBlob()
    {
        var deleted = new List<string>();
        var value = new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg")] };
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(), value,
            MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; }));

        await sut.Images[0].DeleteCommand.ExecuteAsync(null);

        Assert.That(deleted, Is.EqualTo(new[] { "a" }));
        Assert.That(((MultiImageFieldValue)sut.GetCurrentValue()).Pictures.Select(p => p.Key), Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public void MoveUp_ReordersTowardFront()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg"), Pic("c", "c.jpg")] }, MakeContext());

        sut.MoveUpCommand.Execute(sut.Images[2]);

        Assert.That(((MultiImageFieldValue)sut.GetCurrentValue()).Pictures.Select(p => p.Key), Is.EqualTo(new[] { "a", "c", "b" }));
    }

    [Test]
    public async Task RemoveImage_WhenEntryNotInGallery_DoesNotDeleteBlob()
    {
        var deleted = new List<string>();
        var context = MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; });
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg")] }, context);
        var foreign = new MultiImageEntryViewModel("z", "z.jpg", null, context, _ => Task.CompletedTask);

        await sut.RemoveImageCommand.ExecuteAsync(foreign);

        Assert.That(deleted, Is.Empty);
        Assert.That(sut.Images, Has.Count.EqualTo(1));
    }

    [Test]
    public void HasImages_RaisesPropertyChanged_WhenImageAdded()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue(), MakeContext());
        var raised = false;
        sut.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(sut.HasImages)) raised = true; };

        sut.Images.Add(new MultiImageEntryViewModel("a", "a.jpg", null, MakeContext(), _ => Task.CompletedTask));

        Assert.That(raised, Is.True);
    }

    [Test]
    public void MoveUp_OnFirstEntry_LeavesOrderUnchanged()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg")] }, MakeContext());

        sut.MoveUpCommand.Execute(sut.Images[0]);

        Assert.That(((MultiImageFieldValue)sut.GetCurrentValue()).Pictures.Select(p => p.Key), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void HasImages_IsFalse_WhenGalleryIsEmpty()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue(), MakeContext());

        Assert.That(sut.HasImages, Is.False);
    }

    [Test]
    public async Task AddImage_WhenPickReturnsNull_AddsNothing()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue(), MakeContext());

        await sut.AddImageCommand.ExecuteAsync(null);

        Assert.That(sut.Images, Is.Empty);
    }

    [Test]
    public async Task AddImage_WhenPickReturnsPicture_AddsEntryWithKeyAndFileName()
    {
        var context = new ItemEditingContext(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(("new-key", "scan.png", null!)),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(), new MultiImageFieldValue(), context);

        await sut.AddImageCommand.ExecuteAsync(null);

        Assert.That(sut.Images.Single().Key, Is.EqualTo("new-key"));
        Assert.That(sut.Images.Single().FileName, Is.EqualTo("scan.png"));
    }
}
