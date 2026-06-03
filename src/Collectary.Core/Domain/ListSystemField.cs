namespace Collectary.Core.Domain;

public class ListSystemField
{
    public Guid ListFieldDefinitionId { get; set; }
    public Guid SystemFieldId { get; set; }
    public Guid? GroupId { get; set; }
    public int DisplayOrder { get; set; }
    public SystemField SystemField { get; set; } = null!;
}
