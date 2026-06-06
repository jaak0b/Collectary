using System.Globalization;
using System.Text.RegularExpressions;

namespace Collectary.Core.Domain.Fields;

/// <summary>A physical measurement — a number plus a length unit (diameter, case size, dimensions, scale).</summary>
[LocalizedName("FieldType_Measurement")]
[FieldIcon(IconGlyphs.Ruler)]
[FieldCatalog(15, FieldCategory.TextAndNumbers)]
public class MeasurementFieldDefinition : FieldDefinition<MeasurementFieldValue>, IListDisplayable, ITextImportable
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 310;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var match = Regex.Match(raw.Trim(), @"^(?<amount>[-+]?\d[\d.,\s]*)\s*(?<unit>[^\d\s].*)$");
        if (!match.Success || !decimal.TryParse(match.Groups["amount"].Value.Trim(), NumberStyles.Number, culture, out var amount))
            return false;
        value = new MeasurementFieldValue { FieldDefinitionId = Id, Amount = amount, Unit = match.Groups["unit"].Value.Trim() };
        return true;
    }
}

public class MeasurementFieldValue : FieldValue<MeasurementFieldDefinition>
{
    public decimal? Amount { get; set; }
    public string Unit { get; set; } = "mm";

    public override bool IsEmpty => Amount is null;

    public override void CopyFrom(FieldValue source)
    {
        if (source is MeasurementFieldValue s)
        {
            Amount = s.Amount;
            Unit = s.Unit;
        }
    }

    public override string ToString() =>
        Amount.HasValue ? $"{Amount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Unit}" : "";
}
