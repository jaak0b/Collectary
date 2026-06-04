using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

/// <summary>
/// One addable field type in the "Add field" menu. <see cref="Name"/> and <see cref="Icon"/> are read
/// live from the type's <c>[LocalizedName]</c>/<c>[FieldIcon]</c> attributes so the menu re-localizes
/// when the language changes.
/// </summary>
public class FieldTypeCatalogEntry(Type type, FieldCategory category, int order)
{
    public Type Type { get; } = type;
    public FieldCategory Category { get; } = category;
    public int Order { get; } = order;

    public string Name => Type.ToLocalizedString();
    public string Icon => Type.GetFieldIcon();

    /// <summary>Creates a fresh field definition of this type with a localized default label.</summary>
    public FieldDefinition Create()
    {
        var definition = (FieldDefinition)Activator.CreateInstance(Type)!;
        definition.Label = string.Format(LocalizationService.Instance["NewFieldNamed"], Name);
        return definition;
    }
}
