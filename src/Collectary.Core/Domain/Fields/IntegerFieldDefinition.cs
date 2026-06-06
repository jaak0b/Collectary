using System.Globalization;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Integer")]
[FieldIcon(IconGlyphs.NumberSymbol)]
[FieldCatalog(2, FieldCategory.TextAndNumbers)]
public class IntegerFieldDefinition : FieldDefinition<IntegerFieldValue>, IListDisplayable, ITextImportable
{
    public int? Min { get; set; }
    public int? Max { get; set; }
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 20;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!int.TryParse(raw, NumberStyles.Integer | NumberStyles.AllowThousands, culture, out var n)) return false;
        value = new IntegerFieldValue { FieldDefinitionId = Id, Value = n };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is not IntegerFieldDefinition src) return;
        Min = src.Min;
        Max = src.Max;
    }
}

public class IntegerFieldValue : FieldValue<IntegerFieldDefinition>
{
    public int? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is IntegerFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString() ?? "";
}
