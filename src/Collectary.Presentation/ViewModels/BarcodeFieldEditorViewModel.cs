using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

public partial class BarcodeFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly BarcodeFieldDefinition _definition;
    private readonly BarcodeFieldValue _value;
    private readonly ItemEditingContext _context;

    /// <summary>The decoded (or manually entered) code. Editable so the field is never blocked without a camera.</summary>
    [ObservableProperty]
    public partial string? Code { get; set; }

    [ObservableProperty]
    public partial bool CanScanFromCamera { get; set; }

    public string? CameraTooltip => CanScanFromCamera ? null : LocalizationService.Instance["Barcode_NoCameraAvailable"];

    partial void OnCanScanFromCameraChanged(bool value) => OnPropertyChanged(nameof(CameraTooltip));

    public BarcodeFieldEditorViewModel(
        BarcodeFieldDefinition definition,
        BarcodeFieldValue value,
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;
        Code = value.Code;
        CameraAvailabilityResolved = ResolveCameraAvailabilityAsync();
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) => Code = data.Digits(13);

    public Task CameraAvailabilityResolved { get; }

    private async Task ResolveCameraAvailabilityAsync()
    {
        try
        {
            CanScanFromCamera = await _context.IsCameraScanAvailableAsync();
        }
        catch (Exception ex)
        {
            Services.AppLogger.Log.Error(ex, "Probing camera availability failed");
        }
    }

    [RelayCommand]
    private async Task ScanFromFileAsync() => ApplyResult(await _context.ScanBarcodeAsync());

    [RelayCommand]
    private async Task ScanFromCameraAsync() => ApplyResult(await _context.ScanBarcodeFromCameraAsync());

    private void ApplyResult(BarcodeReadResult? result)
    {
        if (result is null) return;
        Code = result.Code;
        _value.Symbology = result.Symbology;
    }

    public override FieldValue GetCurrentValue()
    {
        _value.Code = Code;
        return _value;
    }
}
