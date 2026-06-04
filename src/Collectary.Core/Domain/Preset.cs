namespace Collectary.Core.Domain;

public class Preset : DomainObject, ISyncable
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentPresetId { get; set; }
    public List<FieldDefinition> Fields { get; set; } = new();
    public List<FieldGroup> Groups { get; set; } = new();
    public List<PresetSystemField> SystemFieldRefs { get; set; } = new();
    public int ColumnCount { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public Guid? OwnerId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long Revision { get; set; }
    public long BaseRevision { get; set; }
    public bool IsDirty { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}
