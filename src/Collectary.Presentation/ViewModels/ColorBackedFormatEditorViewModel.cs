using Avalonia.Media;

namespace Collectary.Presentation.ViewModels;

public abstract class ColorBackedFormatEditorViewModel : ColorFormatEditorViewModel
{
    private Color _current;

    protected ColorBackedFormatEditorViewModel(Color initial)
    {
        _current = initial;
    }

    protected Color Current
    {
        get => _current;
        set
        {
            if (_current == value) return;
            _current = value;
            OnCurrentChanged();
        }
    }

    public override bool SupportsPicker => true;

    public override IBrush SwatchBrush => new SolidColorBrush(_current);

    public Color PickerColor
    {
        get => _current;
        set => Current = value;
    }

    protected virtual void OnCurrentChanged()
    {
        OnPropertyChanged(nameof(PickerColor));
        OnPropertyChanged(nameof(SwatchBrush));
    }

    protected void WithChannel(byte? a = null, byte? r = null, byte? g = null, byte? b = null)
    {
        Current = Color.FromArgb(a ?? _current.A, r ?? _current.R, g ?? _current.G, b ?? _current.B);
    }
}
