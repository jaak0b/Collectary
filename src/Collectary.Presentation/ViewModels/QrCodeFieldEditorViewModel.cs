using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class QrCodeFieldEditorViewModel : FieldEditorViewModelBase
{
    private const int MaxContentLength = 500;

    private readonly QrCodeFieldDefinition _definition;
    private readonly QrCodeFieldValue _value;
    private readonly ItemEditingContext _context;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    public partial Bitmap? Preview { get; set; }

    public bool HasPreview => Preview is not null;

    [ObservableProperty]
    public partial string? Content { get; set; }

    public QrCodeFieldEditorViewModel(
        QrCodeFieldDefinition definition,
        QrCodeFieldValue value,
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;
        Content = value.Content;
        Regenerate();
    }

    partial void OnContentChanged(string? value)
    {
        if (value is { Length: > MaxContentLength })
        {
            Content = value[..MaxContentLength];
            return;
        }
        Regenerate();
    }

    private void Regenerate()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            Preview = null;
            return;
        }
        try
        {
            Preview = _context.GenerateQrBitmap(Content);
        }
        catch (Exception ex)
        {
            Preview = null;
            AppLogger.Log.Warning(ex, "QR preview generation failed for content length {Length}", Content.Length);
        }
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Content = Content;
        return _value;
    }
}
