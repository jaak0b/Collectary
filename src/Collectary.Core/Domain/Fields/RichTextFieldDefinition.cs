namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_RichText")]
[FieldIcon(IconGlyphs.TextEditStyle)]
[FieldCatalog(1, FieldCategory.TextAndNumbers)]
public class RichTextFieldDefinition : FieldDefinition<RichTextFieldValue>, IListDisplayable, ITextImportable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (string.IsNullOrWhiteSpace(raw)) return false;
        value = new RichTextFieldValue { FieldDefinitionId = Id, Value = raw };
        return true;
    }
}

public class RichTextFieldValue : FieldValue<RichTextFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is RichTextFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
