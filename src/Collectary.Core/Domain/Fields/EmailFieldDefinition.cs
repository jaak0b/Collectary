namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Email")]
[FieldIcon(IconGlyphs.Mail)]
[FieldCatalog(12, FieldCategory.TextAndNumbers)]
public class EmailFieldDefinition : FieldDefinition<EmailFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }
}

public class EmailFieldValue : FieldValue<EmailFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is EmailFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
