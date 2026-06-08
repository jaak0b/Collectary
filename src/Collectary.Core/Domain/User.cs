namespace Collectary.Core.Domain;

public class User : DomainObject, ISyncable
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long Revision { get; set; }
    public long BaseRevision { get; set; }
    public bool IsDirty { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}
