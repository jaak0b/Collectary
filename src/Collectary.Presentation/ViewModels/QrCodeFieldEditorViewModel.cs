using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class QrCodeFieldEditorViewModel : FieldEditorViewModelBase
{
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

    partial void OnContentChanged(string? value) => Regenerate();

    private void Regenerate() =>
        Preview = string.IsNullOrWhiteSpace(Content) ? null : _context.GenerateQrBitmap(Content);

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Content = Content;
        return _value;
    }
}
