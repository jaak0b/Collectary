using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class BarcodeFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly BarcodeFieldDefinition _definition;
    private readonly BarcodeFieldValue _value;
    private readonly ItemEditingContext _context;

    /// <summary>The decoded (or manually entered) code. Editable so the field is never blocked without a camera.</summary>
    [ObservableProperty]
    public partial string? Code { get; set; }

    public BarcodeFieldEditorViewModel(
        BarcodeFieldDefinition definition,
        BarcodeFieldValue value,
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;
        Code = value.Code;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) => Code = data.Digits(13);

    [RelayCommand]
    private async Task ScanAsync()
    {
        var result = await _context.ScanBarcodeAsync();
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
