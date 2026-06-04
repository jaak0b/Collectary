using Avalonia.Media;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Converters;

namespace Collectary.Presentation.ViewModels;

public class RgbColorFormatEditorViewModel : ColorBackedFormatEditorViewModel
{
    public RgbColorFormatEditorViewModel(string? raw)
        : base(ColorFormatHelper.ToColor(raw, ColorFormat.Rgb) ?? Colors.White)
    {
    }

    public int? R { get => Current.R; set => WithChannel(r: ToByte(value)); }
    public int? G { get => Current.G; set => WithChannel(g: ToByte(value)); }
    public int? B { get => Current.B; set => WithChannel(b: ToByte(value)); }

    protected override void OnCurrentChanged()
    {
        base.OnCurrentChanged();
        OnPropertyChanged(nameof(R));
        OnPropertyChanged(nameof(G));
        OnPropertyChanged(nameof(B));
    }

    public override string Encode() => ColorFormatHelper.Encode(Current, ColorFormat.Rgb);

    private static byte ToByte(int? value) => (byte)Math.Clamp(value ?? 0, 0, 255);
}
