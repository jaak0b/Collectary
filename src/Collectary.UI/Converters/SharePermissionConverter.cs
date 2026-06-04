using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Collectary.Core.Domain;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Converters;

public class SharePermissionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SharePermission permission)
            return LocalizationService.Instance[permission == SharePermission.Edit ? "Permission_Edit" : "Permission_Read"];
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
