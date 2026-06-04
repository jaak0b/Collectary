namespace Collectary.Core.Domain.Fields;

/// <summary>
/// Marks a <see cref="FieldDefinition"/> subtype as user-addable and declares where it sits in the
/// "Add field" menu. Types without this attribute (e.g. <see cref="DisplayNameFieldDefinition"/>) are
/// excluded from the catalog. Each type self-describes here, so adding a new field type needs no menu edits.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class FieldCatalogAttribute(int order, FieldCategory category) : Attribute
{
    /// <summary>Sort order within the <see cref="Category"/> (ascending).</summary>
    public int Order { get; } = order;

    public FieldCategory Category { get; } = category;
}
