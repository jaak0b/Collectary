using System.Reflection;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.Localization;

public static class FieldMetadataExtensions
{
    public static string ToLocalizedString(this Type type)
    {
        var attr = type.GetCustomAttribute<LocalizedNameAttribute>()
            ?? throw new InvalidOperationException(
                $"Type '{type.Name}' has no [LocalizedName] attribute. Add it to the class declaration.");
        return LocalizationService.Instance[attr.Key];
    }

    public static string GetFieldIcon(this Type type)
    {
        var attr = type.GetCustomAttribute<FieldIconAttribute>()
            ?? throw new InvalidOperationException(
                $"Type '{type.Name}' has no [FieldIcon] attribute. Add it to the class declaration.");
        return attr.Icon;
    }
}
