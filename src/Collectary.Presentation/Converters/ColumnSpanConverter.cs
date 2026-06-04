using System.Globalization;
using Avalonia.Data.Converters;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.Converters;

public class ColumnSpanConverter : IValueConverter
{
    public static readonly ColumnSpanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int span) return value;
        return span == 1
            ? LocalizationService.Instance["ColumnSpan_1"]
            : string.Format(LocalizationService.Instance["ColumnSpan_N"], span);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
