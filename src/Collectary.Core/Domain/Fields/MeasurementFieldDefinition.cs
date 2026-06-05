namespace Collectary.Core.Domain.Fields;

/// <summary>A physical measurement — a number plus a length unit (diameter, case size, dimensions, scale).</summary>
[LocalizedName("FieldType_Measurement")]
[FieldIcon(IconGlyphs.Ruler)]
[FieldCatalog(14, FieldCategory.TextAndNumbers)]
public class MeasurementFieldDefinition : FieldDefinition<MeasurementFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
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
