namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Date")]
[FieldIcon(IconGlyphs.Calendar)]
[FieldCatalog(6, FieldCategory.TextAndNumbers)]
public class DateFieldDefinition : FieldDefinition<DateFieldValue>, IListDisplayable
{
    public DateTime? Min { get; set; }
    public DateTime? Max { get; set; }
    public bool ShowInList { get; set; }
}

public class DateFieldValue : FieldValue<DateFieldDefinition>
{
    public DateTime? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is DateFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString("d") ?? "";
}
