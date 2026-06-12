using System.Globalization;
using Avalonia.Data.Converters;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.Converters;

public class LocalizedEnumConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Enum e ? LocalizationService.Instance[$"{e.GetType().Name}_{e}"] : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
