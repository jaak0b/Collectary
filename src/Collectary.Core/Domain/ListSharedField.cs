namespace Collectary.Core.Domain;

public class ListSharedField
{
    public Guid ListFieldDefinitionId { get; set; }
    public Guid SharedFieldId { get; set; }
    public Guid? GroupId { get; set; }
    public int DisplayOrder { get; set; }
    public SharedField SharedField { get; set; } = null!;
}
