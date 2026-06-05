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

    void StampModified(Guid? userId)
    {
        IsDirty = true;
        Revision++;
        if (userId is { } id) LastModifiedByUserId = id;
    }

    void StampDeleted(Guid? userId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        StampModified(userId);
    }

    void MarkPulled()
    {
        BaseRevision = Revision;
        IsDirty = false;
    }
}
