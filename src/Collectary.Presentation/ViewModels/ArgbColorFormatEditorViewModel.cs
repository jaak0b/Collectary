using Avalonia.Media;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Converters;

namespace Collectary.Presentation.ViewModels;

public class ArgbColorFormatEditorViewModel : ColorBackedFormatEditorViewModel
{
    public ArgbColorFormatEditorViewModel(string? raw)
        : base(ColorFormatHelper.ToColor(raw, ColorFormat.Argb) ?? Colors.White)
    {
    }

    public int? A { get => Current.A; set => WithChannel(a: ToByte(value)); }
    public int? R { get => Current.R; set => WithChannel(r: ToByte(value)); }
    public int? G { get => Current.G; set => WithChannel(g: ToByte(value)); }
    public int? B { get => Current.B; set => WithChannel(b: ToByte(value)); }

    protected override void OnCurrentChanged()
    {
        base.OnCurrentChanged();
        OnPropertyChanged(nameof(A));
        OnPropertyChanged(nameof(R));
        OnPropertyChanged(nameof(G));
        OnPropertyChanged(nameof(B));
    }

    public override string Encode() => ColorFormatHelper.Encode(Current, ColorFormat.Argb);

    private static byte ToByte(int? value) => (byte)Math.Clamp(value ?? 0, 0, 255);
}
