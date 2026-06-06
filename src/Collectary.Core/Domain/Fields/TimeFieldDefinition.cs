using System.Globalization;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Time")]
[FieldIcon(IconGlyphs.Clock)]
[FieldCatalog(8, FieldCategory.TextAndNumbers)]
public class TimeFieldDefinition : FieldDefinition<TimeFieldValue>, IListDisplayable, ITextImportable
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 60;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (string.IsNullOrWhiteSpace(raw) || !TimeSpan.TryParse(raw, culture, out _)) return false;
        value = new TimeFieldValue { FieldDefinitionId = Id, Value = raw.Trim() };
        return true;
    }
}

public class TimeFieldValue : FieldValue<TimeFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is TimeFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
