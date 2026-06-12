using FakeItEasy;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

/// <summary>The device/IO hooks default to safe no-ops so an editor never crashes when a capability is absent.</summary>
[TestFixture]
public class ItemEditingContextDefaultsTest
{
    private static ItemEditingContext Make() => new(
        editorRegistry: A.Fake<IFieldEditorRegistry>(),
        listCellBuilder: A.Fake<IListCellBuilder>(),
        goBack: () => { },
        pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
        exportImageAsync: (_, _) => Task.CompletedTask,
        loadImageBitmap: _ => null,
        deleteImageAsync: _ => Task.CompletedTask);

    [Test]
    public async Task ScanBarcode_DefaultsToNull() =>
        Assert.That(await Make().ScanBarcodeAsync(), Is.Null);

    [Test]
    public void GenerateQr_DefaultsToNull() =>
        Assert.That(Make().GenerateQrBitmap("x"), Is.Null);

    [Test]
    public async Task PickFile_DefaultsToNull() =>
        Assert.That(await Make().PickAndStoreFileAsync(), Is.Null);

    [Test]
    public async Task ExportAndDeleteFile_DefaultToNoOp()
    {
        var ctx = Make();
        await ctx.ExportFileAsync("k", "f");
        await ctx.DeleteFileAsync("k");
        Assert.Pass();
    }

    [Test]
    public async Task LoadLinkableItems_DefaultsToEmpty() =>
        Assert.That(await Make().LoadLinkableItemsAsync(), Is.Empty);

    [Test]
    public async Task LoadUsedNumbers_DefaultsToEmpty() =>
        Assert.That(await Make().LoadUsedNumbersAsync(Guid.NewGuid()), Is.Empty);

    [Test]
    public void AudioRecorder_DefaultsToNull() =>
        Assert.That(Make().AudioRecorder, Is.Null);

    [Test]
    public void AudioPlayer_DefaultsToNull() =>
        Assert.That(Make().AudioPlayer, Is.Null);

    [Test]
    public async Task StoreAudio_DefaultsToEmptyKey() =>
        Assert.That(await Make().StoreAudioAsync(Stream.Null), Is.Empty);

    [Test]
    public void OpenAudio_DefaultsToNull() =>
        Assert.That(Make().OpenAudioStream("k"), Is.Null);
}
