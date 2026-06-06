namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Text")]
[FieldIcon(IconGlyphs.TextField)]
[FieldCatalog(0, FieldCategory.TextAndNumbers)]
public class TextFieldDefinition : FieldDefinition<TextFieldValue>, IListDisplayable, ITextImportable
{
    public override int DefaultColumnSpan => 2;
    public int? MaxLength { get; set; }
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = CreateEmptyValue();
            return false;
        }
        value = new TextFieldValue { FieldDefinitionId = Id, Value = raw };
        return true;
    }

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
