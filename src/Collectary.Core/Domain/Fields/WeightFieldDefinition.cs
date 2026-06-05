namespace Collectary.Core.Domain.Fields;

/// <summary>A weight — a number plus a mass unit (grams, ounces, kilograms, pounds).</summary>
[LocalizedName("FieldType_Weight")]
[FieldIcon("⚖")]
[FieldCatalog(15, FieldCategory.TextAndNumbers)]
public class WeightFieldDefinition : FieldDefinition<WeightFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
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
