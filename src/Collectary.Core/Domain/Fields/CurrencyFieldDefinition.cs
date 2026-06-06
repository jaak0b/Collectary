using System.Globalization;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Currency")]
[FieldIcon(IconGlyphs.Money)]
[FieldCatalog(5, FieldCategory.TextAndNumbers)]
public class CurrencyFieldDefinition : FieldDefinition<CurrencyFieldValue>, IListDisplayable, ITextImportable
{
    public bool ShowInList { get; set; }
    public string CurrencySymbol { get; set; } = "€";

    public int ImportInferenceOrder => 50;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var withoutSymbol = string.IsNullOrEmpty(CurrencySymbol) ? raw : raw.Replace(CurrencySymbol, "");
        if (!decimal.TryParse(raw, NumberStyles.Currency, culture, out var d)
            && !decimal.TryParse(withoutSymbol, NumberStyles.Currency, culture, out d))
            return false;
        value = new CurrencyFieldValue { FieldDefinitionId = Id, Value = d };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is CurrencyFieldDefinition src) CurrencySymbol = src.CurrencySymbol;
    }
}

public class CurrencyFieldValue : FieldValue<CurrencyFieldDefinition>
{
    public decimal? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is CurrencyFieldValue s) Value = s.Value; }
    public override string ToString() => Value.HasValue ? $"{Value:F2}" : "";
}
