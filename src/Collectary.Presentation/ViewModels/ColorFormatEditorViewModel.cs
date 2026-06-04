using Avalonia.Media;

namespace Collectary.Presentation.ViewModels;

public abstract class ColorFormatEditorViewModel : ViewModelBase
{
    public abstract bool SupportsPicker { get; }
    public abstract IBrush SwatchBrush { get; }
    public abstract string Encode();
}
