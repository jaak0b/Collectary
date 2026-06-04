using System.Globalization;
using Avalonia.Data.Converters;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.Converters;

public class ImageSizeModeConverter : IValueConverter
{
    public static readonly ImageSizeModeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ImageSizeMode m ? LocalizationService.Instance[$"ImageSizeMode_{m}"] : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
