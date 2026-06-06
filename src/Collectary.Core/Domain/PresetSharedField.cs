namespace Collectary.Core.Domain;

public class PresetSharedField
{
    public Guid PresetId { get; set; }
    public Guid SharedFieldId { get; set; }
    public Guid? GroupId { get; set; }
    public int DisplayOrder { get; set; }
    public SharedField SharedField { get; set; } = null!;
}
