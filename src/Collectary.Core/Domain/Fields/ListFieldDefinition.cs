namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_List")]
[FieldIcon(IconGlyphs.BulletList)]
[FieldCatalog(0, FieldCategory.Structural)]
public class ListFieldDefinition : FieldDefinition<ListFieldValue>
{
    public override int DefaultColumnSpan => 2;
    public int ColumnCount { get; set; } = 1;
    public List<FieldDefinition> SubFields { get; set; } = new();
    public List<FieldGroup> Groups { get; set; } = new();
    public List<ListSharedField> SharedFieldRefs { get; set; } = new();
    public ListInlineStyle InlineStyle { get; set; } = ListInlineStyle.Card;

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is ListFieldDefinition src)
        {
            ColumnCount = src.ColumnCount;
            InlineStyle = src.InlineStyle;
        }
    }
}

public class ListFieldValue : FieldValue<ListFieldDefinition>
{
    public List<ListEntry> Entries { get; set; } = new();
    public override bool IsEmpty => Entries.Count == 0;
    public override void CopyFrom(FieldValue source)
    {
        if (source is ListFieldValue s) Entries = s.Entries;
    }

    public override IEnumerable<string> ReferencedBlobKeys() =>
        Entries.SelectMany(e => e.SubValues).SelectMany(v => v.ReferencedBlobKeys());
}
