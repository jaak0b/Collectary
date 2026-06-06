namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Time")]
[FieldIcon(IconGlyphs.Clock)]
[FieldCatalog(8, FieldCategory.TextAndNumbers)]
public class TimeFieldDefinition : FieldDefinition<TimeFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
}

public class TimeFieldValue : FieldValue<TimeFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is TimeFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
