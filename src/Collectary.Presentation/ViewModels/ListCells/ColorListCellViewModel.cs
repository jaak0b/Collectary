using Avalonia.Media;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Converters;

namespace Collectary.UI.ViewModels.ListCells;

public class ColorListCellViewModel : ListCellViewModelBase
{
    public string SwatchHex { get; }

    public ColorListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        var raw = source is ColorFieldValue cfv ? cfv.Value : null;
        var format = definition is ColorFieldDefinition cfd ? cfd.Format : ColorFormat.Hex;
        var color = ColorFormatHelper.ToColor(raw, format) ?? Colors.Transparent;
        SwatchHex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
