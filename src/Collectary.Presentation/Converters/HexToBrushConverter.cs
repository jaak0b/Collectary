using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Collectary.UI.Converters;

public class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try { return new SolidColorBrush(Color.Parse(hex)); }
            catch (Exception ex) { Services.AppLogger.Log.Debug(ex, "Could not parse color hex '{Hex}'", hex); }
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
