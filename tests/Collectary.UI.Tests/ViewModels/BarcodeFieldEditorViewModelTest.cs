using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class BarcodeFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(Func<Task<BarcodeReadResult?>>? scan = null)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        if (scan is not null) ctx.ScanBarcodeAsync = scan;
        return ctx;
    }

    [Test]
    public void LoadsExistingCode()
    {
        var value = new BarcodeFieldValue { Code = "5901234123457", Symbology = BarcodeSymbology.Ean13 };
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), value, MakeContext());
        Assert.That(sut.Code, Is.EqualTo("5901234123457"));
    }

    [Test]
    public void GetCurrentValue_PersistsCodeAndSymbology()
    {
        var value = new BarcodeFieldValue();
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), value,
            MakeContext(scan: () => Task.FromResult<BarcodeReadResult?>(
                new BarcodeReadResult("ABC-123", BarcodeSymbology.Code128))));

        sut.Code = "manual-entry";
        var persisted = (BarcodeFieldValue)sut.GetCurrentValue();

        Assert.That(persisted.Code, Is.EqualTo("manual-entry"));
    }

    [Test]
    public async Task Scan_SetsCodeAndSymbologyFromResult()
    {
        var value = new BarcodeFieldValue();
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), value,
            MakeContext(scan: () => Task.FromResult<BarcodeReadResult?>(
                new BarcodeReadResult("9780262033848", BarcodeSymbology.Ean13))));

        await sut.ScanCommand.ExecuteAsync(null);

        Assert.That(sut.Code, Is.EqualTo("9780262033848"));
        Assert.That(((BarcodeFieldValue)sut.GetCurrentValue()).Symbology, Is.EqualTo(BarcodeSymbology.Ean13));
    }

    [Test]
    public async Task Scan_WhenNoCodeFound_LeavesExistingCodeUntouched()
    {
        var value = new BarcodeFieldValue { Code = "keep-me", Symbology = BarcodeSymbology.QrCode };
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), value,
            MakeContext(scan: () => Task.FromResult<BarcodeReadResult?>(null)));

        await sut.ScanCommand.ExecuteAsync(null);

        Assert.That(sut.Code, Is.EqualTo("keep-me"));
    }
}
