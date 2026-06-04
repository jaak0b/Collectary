using System.Reflection;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

/// <summary>
/// The single source of truth for the "Add field" menu. Discovers every <see cref="FieldDefinition"/>
/// subtype carrying a <see cref="FieldCatalogAttribute"/> by reflection over the Core assembly, ordered
/// by (<see cref="FieldCategory"/>, order). Both the preset editor and the system-field library render
/// these entries, so the two menus can never diverge. Adding a new field type needs no menu edits.
/// </summary>
public class FieldTypeCatalog
{
    public IReadOnlyList<FieldTypeCatalogEntry> Entries { get; }

    public FieldTypeCatalog()
    {
        Entries = typeof(FieldDefinition).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(FieldDefinition).IsAssignableFrom(t))
            .Select(t => (Type: t, Attribute: t.GetCustomAttribute<FieldCatalogAttribute>()))
            .Where(x => x.Attribute is not null)
            .OrderBy(x => x.Attribute!.Category)
            .ThenBy(x => x.Attribute!.Order)
            .Select(x => new FieldTypeCatalogEntry(x.Type, x.Attribute!.Category, x.Attribute!.Order))
            .ToList();
    }
}
