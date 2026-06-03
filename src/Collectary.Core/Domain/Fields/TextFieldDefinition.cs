namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Text")]
[FieldIcon("🔤")]
public class TextFieldDefinition : FieldDefinition<TextFieldValue>, IListDisplayable
{
    public int? MaxLength { get; set; }
    public bool ShowInList { get; set; }
}

public class TextFieldValue : FieldValue<TextFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is TextFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
