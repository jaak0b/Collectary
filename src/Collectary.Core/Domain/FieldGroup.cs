using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Domain;

public class FieldGroup : DomainObject
{
    public Guid? PresetId { get; set; }
    public Guid? ParentListFieldDefinitionId { get; set; }
    public Guid? ParentGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public GroupDisplayMode DisplayMode { get; set; } = GroupDisplayMode.Card;
    public int ColumnCount { get; set; } = 1;
    public bool DefaultCollapsed { get; set; }
    public bool ShowInList { get; set; } = true;
    public bool PrefixColumnHeaders { get; set; }
}
