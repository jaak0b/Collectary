using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class ImageFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly ImageFieldDefinition _definition;
    private readonly ImageFieldValue _value;
    private readonly ItemEditingContext _context;

    [ObservableProperty]
    public partial Bitmap? ImageBitmap { get; set; }

    [ObservableProperty]
    public partial bool HasImage { get; set; }

    private ImageSizeMode SizeMode => _definition.SizeMode;

    public double BorderFixedWidth  => SizeMode == ImageSizeMode.Fixed ? _definition.DisplayWidth : double.NaN;
    public double BorderFixedHeight => SizeMode == ImageSizeMode.Fixed ? _definition.DisplayHeight : double.NaN;
    public double BorderMinWidth    => SizeMode == ImageSizeMode.Min ? Math.Max(10, _definition.DisplayWidth) : 10;
    public double BorderMinHeight   => SizeMode == ImageSizeMode.Min ? Math.Max(10, _definition.DisplayHeight) : 10;
    public double BorderMaxWidth    => SizeMode == ImageSizeMode.Max ? _definition.DisplayWidth : double.PositiveInfinity;
    public double BorderMaxHeight   => SizeMode == ImageSizeMode.Max ? _definition.DisplayHeight : double.PositiveInfinity;

    public override FieldDefinition Definition => _definition;

    public ImageFieldEditorViewModel(
        ImageFieldDefinition definition,
        ImageFieldValue value,
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;

        if (!string.IsNullOrEmpty(value.ImageKey))
        {
            ImageBitmap = context.LoadImageBitmap(value.ImageKey);
            HasImage = ImageBitmap is not null;
        }
    }

    [RelayCommand]
    private async Task SelectImageAsync()
    {
        var result = await _context.PickAndStoreImageAsync();
        if (result is null) return;
        _value.ImageKey = result.Value.Key;
        _value.FileName = result.Value.FileName;
        ImageBitmap = result.Value.Preview;
        HasImage = true;
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (string.IsNullOrEmpty(_value.ImageKey)) return;
        await _context.ExportImageAsync(_value.ImageKey, _value.FileName ?? "image");
    }

    [RelayCommand]
    private async Task DeleteImageAsync()
    {
        if (string.IsNullOrEmpty(_value.ImageKey)) return;
        await _context.DeleteImageAsync(_value.ImageKey);
        _value.ImageKey = null;
        _value.FileName = null;
        ImageBitmap = null;
        HasImage = false;
    }

    public override FieldValue GetCurrentValue() => _value;
}
