namespace Collectary.Core.Domain;

public interface ISyncable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    long Revision { get; set; }
    long BaseRevision { get; set; }
    bool IsDirty { get; set; }
    Guid? LastModifiedByUserId { get; set; }
    DateTime UpdatedAt { get; set; }
}
