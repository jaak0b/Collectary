using System.Globalization;
using System.Text.RegularExpressions;

namespace Collectary.Core.Domain.Fields;

/// <summary>A weight — a number plus a mass unit (grams, ounces, kilograms, pounds).</summary>
[LocalizedName("FieldType_Weight")]
[FieldIcon(IconGlyphs.Scales)]
[FieldCatalog(16, FieldCategory.TextAndNumbers)]
public class WeightFieldDefinition : FieldDefinition<WeightFieldValue>, IListDisplayable, ITextImportable
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 320;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var match = Regex.Match(raw.Trim(), @"^(?<amount>[-+]?\d[\d.,\s]*)\s*(?<unit>[^\d\s].*)$");
        if (!match.Success || !decimal.TryParse(match.Groups["amount"].Value.Trim(), NumberStyles.Number, culture, out var amount))
            return false;
        value = new WeightFieldValue { FieldDefinitionId = Id, Amount = amount, Unit = match.Groups["unit"].Value.Trim() };
        return true;
    }
}

public class WeightFieldValue : FieldValue<WeightFieldDefinition>
{
    public decimal? Amount { get; set; }
    public string Unit { get; set; } = "g";

    public override bool IsEmpty => Amount is null;

    public override void CopyFrom(FieldValue source)
    {
        if (source is WeightFieldValue s)
        {
            Amount = s.Amount;
            Unit = s.Unit;
        }
    }

    public override string ToString() =>
        Amount.HasValue ? $"{Amount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Unit}" : "";
}
