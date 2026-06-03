using Avalonia.Media;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Converters;

namespace Collectary.UI.ViewModels;

public class HexColorFormatEditorViewModel : ColorBackedFormatEditorViewModel
{
    public HexColorFormatEditorViewModel(string? raw)
        : base(ColorFormatHelper.ToColor(raw, ColorFormat.Hex) ?? Colors.White)
    {
    }

    public string Hex
    {
        get => $"#{Current.R:X2}{Current.G:X2}{Current.B:X2}";
        set
        {
            if (ColorFormatHelper.ToColor(value, ColorFormat.Hex) is { } c)
                Current = Color.FromRgb(c.R, c.G, c.B);
        }
    }

    protected override void OnCurrentChanged()
    {
        base.OnCurrentChanged();
        OnPropertyChanged(nameof(Hex));
    }

    public override string Encode() => ColorFormatHelper.Encode(Current, ColorFormat.Hex);
}
