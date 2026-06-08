namespace Collectary.Core.Domain;

public class CollectionShare : DomainObject, ISyncable
{
    public Guid PresetId { get; set; }
    public Guid SharedWithUserId { get; set; }
    public Guid GrantedByUserId { get; set; }
    public SharePermission Permission { get; set; } = SharePermission.Read;
    public DateTime GrantedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long Revision { get; set; }
    public long BaseRevision { get; set; }
    public bool IsDirty { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}
