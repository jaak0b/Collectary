namespace Collectary.Core.Domain;

public class ListEntry : DomainObject
{
    public Guid ListFieldValueId { get; set; }
    public int DisplayOrder { get; set; }
    public List<FieldValue> SubValues { get; set; } = new();
}
