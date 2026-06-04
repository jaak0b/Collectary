using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ImageFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(
        Func<string, Avalonia.Media.Imaging.Bitmap?>? load = null,
        Func<string, string, Task>? export = null,
        Func<string, Task>? delete = null)
        => new(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: export ?? ((_, _) => Task.CompletedTask),
            loadImageBitmap: load ?? (_ => null),
            deleteImageAsync: delete ?? (_ => Task.CompletedTask));

    [Test]
    public void Geometry_Fixed()
    {
        var def = new ImageFieldDefinition { SizeMode = ImageSizeMode.Fixed, DisplayWidth = 120, DisplayHeight = 80 };
        var sut = new ImageFieldEditorViewModel(def, new ImageFieldValue(), MakeContext());
        Assert.That(sut.BorderFixedWidth, Is.EqualTo(120));
        Assert.That(sut.BorderFixedHeight, Is.EqualTo(80));
        Assert.That(sut.BorderMaxWidth, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Geometry_Min()
    {
        var def = new ImageFieldDefinition { SizeMode = ImageSizeMode.Min, DisplayWidth = 5, DisplayHeight = 300 };
        var sut = new ImageFieldEditorViewModel(def, new ImageFieldValue(), MakeContext());
        Assert.That(sut.BorderMinWidth, Is.EqualTo(10));
        Assert.That(sut.BorderMinHeight, Is.EqualTo(300));
        Assert.That(double.IsNaN(sut.BorderFixedWidth), Is.True);
    }

    [Test]
    public void Geometry_Max()
    {
        var def = new ImageFieldDefinition { SizeMode = ImageSizeMode.Max, DisplayWidth = 200, DisplayHeight = 150 };
        var sut = new ImageFieldEditorViewModel(def, new ImageFieldValue(), MakeContext());
        Assert.That(sut.BorderMaxWidth, Is.EqualTo(200));
        Assert.That(sut.BorderMaxHeight, Is.EqualTo(150));
        Assert.That(sut.BorderMinWidth, Is.EqualTo(10));
    }

    [Test]
    public void GetCurrentValue_ReturnsBackingValue()
    {
        var value = new ImageFieldValue { ImageKey = "k" };
        var sut = new ImageFieldEditorViewModel(new ImageFieldDefinition(), value, MakeContext());
        Assert.That(sut.GetCurrentValue(), Is.SameAs(value));
    }

    [Test]
    public async Task DeleteImage_ClearsKeyAndCallsContext()
    {
        var deleted = new List<string>();
        var value = new ImageFieldValue { ImageKey = "k", FileName = "f.png" };
        var sut = new ImageFieldEditorViewModel(new ImageFieldDefinition(), value,
            MakeContext(delete: k => { deleted.Add(k); return Task.CompletedTask; }));

        await sut.DeleteImageCommand.ExecuteAsync(null);

        Assert.That(deleted, Is.EqualTo(new[] { "k" }));
        Assert.That(value.ImageKey, Is.Null);
        Assert.That(sut.HasImage, Is.False);
    }

    [Test]
    public async Task DeleteAndSaveAs_NoOpWhenNoImage()
    {
        var deleted = false;
        var exported = false;
        var sut = new ImageFieldEditorViewModel(new ImageFieldDefinition(), new ImageFieldValue(),
            MakeContext(
                delete: _ => { deleted = true; return Task.CompletedTask; },
                export: (_, _) => { exported = true; return Task.CompletedTask; }));

        await sut.DeleteImageCommand.ExecuteAsync(null);
        await sut.SaveAsCommand.ExecuteAsync(null);

        Assert.That(deleted, Is.False);
        Assert.That(exported, Is.False);
    }

    [Test]
    public async Task SaveAs_ExportsWhenImagePresent()
    {
        (string key, string name)? exported = null;
        var value = new ImageFieldValue { ImageKey = "k", FileName = "pic.png" };
        var sut = new ImageFieldEditorViewModel(new ImageFieldDefinition(), value,
            MakeContext(export: (k, n) => { exported = (k, n); return Task.CompletedTask; }));

        await sut.SaveAsCommand.ExecuteAsync(null);

        Assert.That(exported, Is.EqualTo(("k", "pic.png")));
    }

    [Test]
    public void Geometry_Fixed_NonFixedDimensionsUseDefaults()
    {
        var def = new ImageFieldDefinition { SizeMode = ImageSizeMode.Fixed, DisplayWidth = 120, DisplayHeight = 80 };
        var sut = new ImageFieldEditorViewModel(def, new ImageFieldValue(), MakeContext());

        Assert.That(sut.BorderMinWidth, Is.EqualTo(10));
        Assert.That(sut.BorderMinHeight, Is.EqualTo(10));
        Assert.That(sut.BorderMaxHeight, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Geometry_Min_SmallHeightClampsToTen()
    {
        var def = new ImageFieldDefinition { SizeMode = ImageSizeMode.Min, DisplayWidth = 400, DisplayHeight = 3 };
        var sut = new ImageFieldEditorViewModel(def, new ImageFieldValue(), MakeContext());

        Assert.That(sut.BorderMinWidth, Is.EqualTo(400));
        Assert.That(sut.BorderMinHeight, Is.EqualTo(10));
        Assert.That(double.IsNaN(sut.BorderFixedHeight), Is.True);
        Assert.That(sut.BorderMaxWidth, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Geometry_Max_FixedDimensionsAreNaN()
    {
        var def = new ImageFieldDefinition { SizeMode = ImageSizeMode.Max, DisplayWidth = 200, DisplayHeight = 150 };
        var sut = new ImageFieldEditorViewModel(def, new ImageFieldValue(), MakeContext());

        Assert.That(double.IsNaN(sut.BorderFixedWidth), Is.True);
        Assert.That(double.IsNaN(sut.BorderFixedHeight), Is.True);
        Assert.That(sut.BorderMinHeight, Is.EqualTo(10));
    }

    [Test]
    public void Constructor_WithImageKeyButNullLoad_LeavesHasImageFalse()
    {
        var value = new ImageFieldValue { ImageKey = "missing" };
        var sut = new ImageFieldEditorViewModel(new ImageFieldDefinition(), value, MakeContext(load: _ => null));

        Assert.That(sut.HasImage, Is.False);
        Assert.That(sut.ImageBitmap, Is.Null);
    }

    [Test]
    public void Constructor_WithEmptyImageKey_DoesNotCallLoad()
    {
        var loadCalls = 0;
        _ = new ImageFieldEditorViewModel(new ImageFieldDefinition(), new ImageFieldValue { ImageKey = "" },
            MakeContext(load: _ => { loadCalls++; return null; }));

        Assert.That(loadCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task SelectImage_WhenPickReturnsNull_DoesNotSetHasImage()
    {
        var value = new ImageFieldValue();
        var sut = new ImageFieldEditorViewModel(new ImageFieldDefinition(), value, MakeContext());

        await sut.SelectImageCommand.ExecuteAsync(null);

        Assert.That(sut.HasImage, Is.False);
        Assert.That(value.ImageKey, Is.Null);
    }
}
