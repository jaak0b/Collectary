using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class BarcodeFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(
        Func<Task<BarcodeReadResult?>>? scan = null,
        Func<Task<BarcodeReadResult?>>? cameraScan = null,
        Func<Task<bool>>? cameraAvailableAsync = null)
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
        if (cameraScan is not null) ctx.ScanBarcodeFromCameraAsync = cameraScan;
        if (cameraAvailableAsync is not null) ctx.IsCameraScanAvailableAsync = cameraAvailableAsync;
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
    public async Task ScanFromFile_SetsCodeAndSymbologyFromResult()
    {
        var value = new BarcodeFieldValue();
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), value,
            MakeContext(scan: () => Task.FromResult<BarcodeReadResult?>(
                new BarcodeReadResult("9780262033848", BarcodeSymbology.Ean13))));

        await sut.ScanFromFileCommand.ExecuteAsync(null);

        Assert.That(sut.Code, Is.EqualTo("9780262033848"));
        Assert.That(((BarcodeFieldValue)sut.GetCurrentValue()).Symbology, Is.EqualTo(BarcodeSymbology.Ean13));
    }

    [Test]
    public async Task ScanFromFile_WhenNoCodeFound_LeavesExistingCodeUntouched()
    {
        var value = new BarcodeFieldValue { Code = "keep-me", Symbology = BarcodeSymbology.QrCode };
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), value,
            MakeContext(scan: () => Task.FromResult<BarcodeReadResult?>(null)));

        await sut.ScanFromFileCommand.ExecuteAsync(null);

        Assert.That(sut.Code, Is.EqualTo("keep-me"));
    }

    [Test]
    public async Task ScanFromCamera_SetsCodeAndSymbologyFromResult()
    {
        var value = new BarcodeFieldValue();
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), value,
            MakeContext(cameraScan: () => Task.FromResult<BarcodeReadResult?>(
                new BarcodeReadResult("4006381333931", BarcodeSymbology.Ean13))));

        await sut.ScanFromCameraCommand.ExecuteAsync(null);

        Assert.That(sut.Code, Is.EqualTo("4006381333931"));
        Assert.That(((BarcodeFieldValue)sut.GetCurrentValue()).Symbology, Is.EqualTo(BarcodeSymbology.Ean13));
    }

    [Test]
    public async Task ScanFromCamera_WhenCancelled_LeavesExistingCodeUntouched()
    {
        var value = new BarcodeFieldValue { Code = "keep-me", Symbology = BarcodeSymbology.QrCode };
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), value,
            MakeContext(cameraScan: () => Task.FromResult<BarcodeReadResult?>(null)));

        await sut.ScanFromCameraCommand.ExecuteAsync(null);

        Assert.That(sut.Code, Is.EqualTo("keep-me"));
    }

    [Test]
    public void Ctor_DoesNotProbeTheCamera()
    {
        var probed = false;
        new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(), new BarcodeFieldValue(),
            MakeContext(cameraAvailableAsync: () => { probed = true; return Task.FromResult(true); }));

        Assert.That(probed, Is.False,
            "opening an item must never enumerate cameras — that native probe crashes the desktop app");
    }

    [Test]
    public void CanScanFromCamera_IsFalseBeforeProbed()
    {
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(),
            new BarcodeFieldValue(), MakeContext(cameraAvailableAsync: () => Task.FromResult(true)));

        Assert.That(sut.CanScanFromCamera, Is.False);
    }

    [Test]
    public async Task RefreshCameraAvailability_BecomesTrue_WhenContextResolvesAvailable()
    {
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(),
            new BarcodeFieldValue(), MakeContext(cameraAvailableAsync: () => Task.FromResult(true)));

        await sut.RefreshCameraAvailabilityCommand.ExecuteAsync(null);

        Assert.That(sut.CanScanFromCamera, Is.True);
    }

    [Test]
    public async Task RefreshCameraAvailability_StaysFalse_WhenContextResolvesUnavailable()
    {
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(),
            new BarcodeFieldValue(), MakeContext(cameraAvailableAsync: () => Task.FromResult(false)));

        await sut.RefreshCameraAvailabilityCommand.ExecuteAsync(null);

        Assert.That(sut.CanScanFromCamera, Is.False);
    }

    [Test]
    public async Task RefreshCameraAvailability_RechecksEachTime_SoDisconnectingTheCameraDisablesScanning()
    {
        var connected = true;
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(),
            new BarcodeFieldValue(), MakeContext(cameraAvailableAsync: () => Task.FromResult(connected)));

        await sut.RefreshCameraAvailabilityCommand.ExecuteAsync(null);
        Assert.That(sut.CanScanFromCamera, Is.True, "camera connected on the first open");

        connected = false;
        await sut.RefreshCameraAvailabilityCommand.ExecuteAsync(null);
        Assert.That(sut.CanScanFromCamera, Is.False, "re-opening after unplugging must drop the camera option");
    }

    [Test]
    public async Task RefreshCameraAvailability_WhenProbeThrows_StaysFalse()
    {
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(),
            new BarcodeFieldValue(),
            MakeContext(cameraAvailableAsync: () => throw new InvalidOperationException("probe failed")));

        await sut.RefreshCameraAvailabilityCommand.ExecuteAsync(null);

        Assert.That(sut.CanScanFromCamera, Is.False);
    }

    [Test]
    public async Task CameraTooltip_WhenCameraUnavailable_ShowsNoCameraMessage()
    {
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(),
            new BarcodeFieldValue(), MakeContext(cameraAvailableAsync: () => Task.FromResult(false)));

        await sut.RefreshCameraAvailabilityCommand.ExecuteAsync(null);

        Assert.That(sut.CameraTooltip, Is.EqualTo(LocalizationService.Instance["Barcode_NoCameraAvailable"]));
    }

    [Test]
    public async Task CameraTooltip_WhenCameraAvailable_IsNull()
    {
        var sut = new BarcodeFieldEditorViewModel(new BarcodeFieldDefinition(),
            new BarcodeFieldValue(), MakeContext(cameraAvailableAsync: () => Task.FromResult(true)));
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        await sut.RefreshCameraAvailabilityCommand.ExecuteAsync(null);

        Assert.That(sut.CameraTooltip, Is.Null);
        Assert.That(raised, Does.Contain(nameof(BarcodeFieldEditorViewModel.CameraTooltip)));
    }
}
