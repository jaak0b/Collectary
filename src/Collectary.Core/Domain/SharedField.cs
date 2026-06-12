namespace Collectary.Core.Domain;

public class SharedField : DomainObject, ISyncable
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public required FieldDefinition Definition { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Revision { get; set; }
    public long BaseRevision { get; set; }
    public bool IsDirty { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public long Lamport { get; set; }
    public Guid LastModifiedByDeviceId { get; set; }
}
