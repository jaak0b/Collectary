namespace Collectary.Core.Domain;

public class PresetSystemField
{
    public Guid PresetId { get; set; }
    public Guid SystemFieldId { get; set; }
    public Guid? GroupId { get; set; }
    public int DisplayOrder { get; set; }
    public SystemField SystemField { get; set; } = null!;
}
