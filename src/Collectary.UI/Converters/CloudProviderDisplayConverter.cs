using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Collectary.Core.Domain;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Converters;

public class CloudProviderDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CloudProvider provider)
            return LocalizationService.Instance[provider switch
            {
                CloudProvider.OneDrive => "Settings_Provider_OneDrive",
                CloudProvider.GoogleDrive => "Settings_Provider_GoogleDrive",
                _ => "Settings_Provider_Folder",
            }];
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
