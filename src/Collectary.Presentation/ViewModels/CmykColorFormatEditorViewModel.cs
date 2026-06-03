using Avalonia.Media;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Converters;

namespace Collectary.UI.ViewModels;

public class CmykColorFormatEditorViewModel : ColorFormatEditorViewModel
{
    private int _c;
    private int _m;
    private int _y;
    private int _k;

    public CmykColorFormatEditorViewModel(string? raw)
    {
        (_c, _m, _y, _k) = ColorFormatHelper.DecodeCmyk(raw);
    }

    public int? C { get => _c; set => SetComponent(ref _c, value); }
    public int? M { get => _m; set => SetComponent(ref _m, value); }
    public int? Y { get => _y; set => SetComponent(ref _y, value); }
    public int? K { get => _k; set => SetComponent(ref _k, value); }

    public override bool SupportsPicker => false;

    public override IBrush SwatchBrush => new SolidColorBrush(ColorFormatHelper.CmykToColor(_c, _m, _y, _k));

    public override string Encode() => ColorFormatHelper.EncodeCmyk(_c, _m, _y, _k);

    private void SetComponent(ref int field, int? value)
    {
        var clamped = Math.Clamp(value ?? 0, 0, 100);
        if (field == clamped) return;
        field = clamped;
        OnPropertyChanged(nameof(SwatchBrush));
    }
}
