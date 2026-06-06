namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Text")]
[FieldIcon(IconGlyphs.TextField)]
[FieldCatalog(0, FieldCategory.TextAndNumbers)]
public class TextFieldDefinition : FieldDefinition<TextFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public int? MaxLength { get; set; }
    public bool ShowInList { get; set; }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is TextFieldDefinition src) MaxLength = src.MaxLength;
    }
}

public class TextFieldValue : FieldValue<TextFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is TextFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
