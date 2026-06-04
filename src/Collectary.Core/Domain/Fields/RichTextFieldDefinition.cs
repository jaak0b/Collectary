namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_RichText")]
[FieldIcon("📝")]
[FieldCatalog(1, FieldCategory.TextAndNumbers)]
public class RichTextFieldDefinition : FieldDefinition<RichTextFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }
}

public class RichTextFieldValue : FieldValue<RichTextFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is RichTextFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
