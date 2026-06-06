using System.Text.RegularExpressions;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Phone")]
[FieldIcon(IconGlyphs.Call)]
[FieldCatalog(11, FieldCategory.TextAndNumbers)]
public class PhoneFieldDefinition : FieldDefinition<PhoneFieldValue>, IListDisplayable, ITextImportable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 140;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        if (text.Count(char.IsDigit) < 5 || !Regex.IsMatch(text, @"^\+?[0-9\s\-()./]+$")) return false;
        value = new PhoneFieldValue { FieldDefinitionId = Id, Value = text };
        return true;
    }
}

public class PhoneFieldValue : FieldValue<PhoneFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is PhoneFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
