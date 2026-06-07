using System.Text.RegularExpressions;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Color")]
[FieldIcon(IconGlyphs.Color)]
[FieldCatalog(0, FieldCategory.Visual)]
public class ColorFieldDefinition : FieldDefinition<ColorFieldValue>, IListDisplayable, ITextImportable
{
    public override int DefaultColumnSpan => 2;
    public ColorFormat Format { get; set; } = ColorFormat.Hex;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 150;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        var isHex = Regex.IsMatch(text, @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$");
        var isRgb = Regex.IsMatch(text, @"^rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*(?:,\s*(?:\d+(?:\.\d+)?|\.\d+)\s*)?\)$", RegexOptions.IgnoreCase);
        if (!isHex && !isRgb) return false;
        value = new ColorFieldValue { FieldDefinitionId = Id, Value = text };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is ColorFieldDefinition src) Format = src.Format;
    }
}

public class ColorFieldValue : FieldValue<ColorFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrEmpty(Value);
    public override void CopyFrom(FieldValue source) { if (source is ColorFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
