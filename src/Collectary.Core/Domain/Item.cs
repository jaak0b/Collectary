namespace Collectary.Core.Domain;

public class Item : DomainObject
{
    public Guid PresetId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<FieldValue> Values { get; set; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
