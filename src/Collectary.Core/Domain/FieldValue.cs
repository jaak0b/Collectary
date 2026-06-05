namespace Collectary.Core.Domain;

public abstract class FieldValue : DomainObject
{
    public Guid FieldDefinitionId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? ListEntryId { get; set; }
    public abstract bool IsEmpty { get; }
    public abstract void CopyFrom(FieldValue source);
    public virtual IEnumerable<string> ReferencedBlobKeys() => Enumerable.Empty<string>();
}

public abstract class FieldValue<TDefinition> : FieldValue
    where TDefinition : FieldDefinition, new()
{
    public TDefinition? Definition { get; set; }
}
