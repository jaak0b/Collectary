using System.Text.RegularExpressions;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Email")]
[FieldIcon(IconGlyphs.Mail)]
[FieldCatalog(12, FieldCategory.TextAndNumbers)]
public class EmailFieldDefinition : FieldDefinition<EmailFieldValue>, IListDisplayable, ITextImportable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 120;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        if (!Regex.IsMatch(text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return false;
        value = new EmailFieldValue { FieldDefinitionId = Id, Value = text };
        return true;
    }
}

public class EmailFieldValue : FieldValue<EmailFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is EmailFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
