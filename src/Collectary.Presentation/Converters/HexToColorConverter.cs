using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Collectary.Presentation.Converters;

public class HexToColorConverter : IValueConverter
{
    public static readonly HexToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try { return Color.Parse(hex); }
            catch (Exception ex) { Services.AppLogger.Log.Debug(ex, "Could not parse color hex '{Hex}'", hex); }
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
