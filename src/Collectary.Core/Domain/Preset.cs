namespace Collectary.Core.Domain;

public class Preset : DomainObject
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentPresetId { get; set; }
    public List<FieldDefinition> Fields { get; set; } = new();
    public List<FieldGroup> Groups { get; set; } = new();
    public List<PresetSystemField> SystemFieldRefs { get; set; } = new();
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
