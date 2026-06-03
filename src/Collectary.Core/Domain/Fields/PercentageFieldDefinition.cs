namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Percentage")]
[FieldIcon("💯")]
public class PercentageFieldDefinition : FieldDefinition<PercentageFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
}

public class PercentageFieldValue : FieldValue<PercentageFieldDefinition>
{
    public decimal? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is PercentageFieldValue s) Value = s.Value; }
    public override string ToString() => Value.HasValue ? $"{Value:F1} %" : "";
}
