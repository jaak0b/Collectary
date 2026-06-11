namespace Collectary.Core.Domain;

public interface ISyncable
{
    Guid Id { get; }

    long Revision { get; set; }
    long BaseRevision { get; set; }
    bool IsDirty { get; set; }
    Guid? LastModifiedByUserId { get; set; }
    DateTime UpdatedAt { get; set; }

    long Lamport { get; set; }
    Guid LastModifiedByDeviceId { get; set; }

    void StampModified(Guid? userId)
    {
        IsDirty = true;
        Revision++;
        UpdatedAt = DateTime.UtcNow;
        if (userId is { } id) LastModifiedByUserId = id;
    }

    void MarkPulled()
    {
        BaseRevision = Revision;
        IsDirty = false;
    }

    void StampLamport(long lamport, Guid deviceId)
    {
        Lamport = lamport;
        LastModifiedByDeviceId = deviceId;
    }
}
