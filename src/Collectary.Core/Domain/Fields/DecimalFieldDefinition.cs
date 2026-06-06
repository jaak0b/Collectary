using System.Globalization;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Decimal")]
[FieldIcon(IconGlyphs.NumberSymbol)]
[FieldCatalog(3, FieldCategory.TextAndNumbers)]
public class DecimalFieldDefinition : FieldDefinition<DecimalFieldValue>, IListDisplayable, ITextImportable
{
    public int DecimalPlaces { get; set; } = 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 40;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!decimal.TryParse(raw, NumberStyles.Number, culture, out var d)) return false;
        value = new DecimalFieldValue { FieldDefinitionId = Id, Value = d };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is DecimalFieldDefinition src) DecimalPlaces = src.DecimalPlaces;
    }
}

public class DecimalFieldValue : FieldValue<DecimalFieldDefinition>
{
    public decimal? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is DecimalFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString() ?? "";
}
