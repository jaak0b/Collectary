namespace Collectary.Core.Domain;

public class Item : DomainObject, ISyncable
{
    public Guid PresetId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<FieldValue> Values { get; set; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long Revision { get; set; }
    public long BaseRevision { get; set; }
    public bool IsDirty { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public long Lamport { get; set; }
    public Guid LastModifiedByDeviceId { get; set; }
}
