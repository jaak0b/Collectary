namespace Collectary.Core.Domain;

public class CollectionShare : DomainObject
{
    public Guid PresetId { get; set; }
    public Guid SharedWithUserId { get; set; }
    public Guid GrantedByUserId { get; set; }
    public SharePermission Permission { get; set; } = SharePermission.Read;
    public DateTime GrantedAt { get; init; } = DateTime.UtcNow;
}
