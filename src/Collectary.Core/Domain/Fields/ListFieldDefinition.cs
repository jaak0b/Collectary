namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_List")]
[FieldIcon("▤")]
public class ListFieldDefinition : FieldDefinition<ListFieldValue>
{
    public List<FieldDefinition> SubFields { get; set; } = new();
    public List<FieldGroup> Groups { get; set; } = new();
    public List<ListSystemField> SystemFieldRefs { get; set; } = new();
    public ListInlineStyle InlineStyle { get; set; } = ListInlineStyle.Card;
}

public class ListFieldValue : FieldValue<ListFieldDefinition>
{
    public List<ListEntry> Entries { get; set; } = new();
    public override bool IsEmpty => Entries.Count == 0;
    public override void CopyFrom(FieldValue source)
    {
        if (source is ListFieldValue s) Entries = s.Entries;
    }
}
