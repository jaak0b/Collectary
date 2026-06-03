namespace Collectary.Core.Domain;

public class SystemField : DomainObject
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public required FieldDefinition Definition { get; set; }
}
