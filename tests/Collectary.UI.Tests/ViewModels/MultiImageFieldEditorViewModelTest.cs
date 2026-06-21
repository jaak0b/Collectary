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

    [Test]
    public void LoadsExistingKeysInOrder()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue { ImageKeys = ["a", "b", "c"] }, MakeContext());

        Assert.That(sut.Images.Select(i => i.Key), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(sut.HasImages, Is.True);
    }

    [Test]
    public void GetCurrentValue_PersistsKeyOrder()
    {
        var value = new MultiImageFieldValue { ImageKeys = ["a", "b"] };
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(), value, MakeContext());

        var persisted = (MultiImageFieldValue)sut.GetCurrentValue();

        Assert.That(persisted.ImageKeys, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task RemoveImage_DropsEntryAndDeletesBlob()
    {
        var deleted = new List<string>();
        var value = new MultiImageFieldValue { ImageKeys = ["a", "b"] };
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(), value,
            MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; }));

        await sut.RemoveImageCommand.ExecuteAsync(sut.Images[0]);

        Assert.That(deleted, Is.EqualTo(new[] { "a" }));
        Assert.That(((MultiImageFieldValue)sut.GetCurrentValue()).ImageKeys, Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public async Task Entry_SaveAs_ExportsWithTheOriginalFileName()
    {
        string? exportedKey = null;
        string? exportedName = null;
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue { ImageKeys = ["abc-123_photo.jpg"] },
            MakeContext(export: (key, name) => { exportedKey = key; exportedName = name; return Task.CompletedTask; }));

        await sut.Images[0].SaveAsCommand.ExecuteAsync(null);

        Assert.That(exportedKey, Is.EqualTo("abc-123_photo.jpg"));
        Assert.That(exportedName, Is.EqualTo("photo.jpg"));
    }

    [Test]
    public async Task Entry_Delete_RemovesEntryAndDeletesBlob()
    {
        var deleted = new List<string>();
        var value = new MultiImageFieldValue { ImageKeys = ["a", "b"] };
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(), value,
            MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; }));

        await sut.Images[0].DeleteCommand.ExecuteAsync(null);

        Assert.That(deleted, Is.EqualTo(new[] { "a" }));
        Assert.That(((MultiImageFieldValue)sut.GetCurrentValue()).ImageKeys, Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public void MoveUp_ReordersTowardFront()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue { ImageKeys = ["a", "b", "c"] }, MakeContext());

        sut.MoveUpCommand.Execute(sut.Images[2]);

        Assert.That(((MultiImageFieldValue)sut.GetCurrentValue()).ImageKeys, Is.EqualTo(new[] { "a", "c", "b" }));
    }

    [Test]
    public async Task AddImage_WhenPickReturnsNull_AddsNothing()
    {
        var sut = new MultiImageFieldEditorViewModel(new MultiImageFieldDefinition(),
            new MultiImageFieldValue(), MakeContext());

        await sut.AddImageCommand.ExecuteAsync(null);

        Assert.That(sut.Images, Is.Empty);
    }
}
