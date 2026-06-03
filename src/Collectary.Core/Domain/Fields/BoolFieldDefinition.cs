namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Bool")]
[FieldIcon("☑")]
public class BoolFieldDefinition : FieldDefinition<BoolFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
}

public class BoolFieldValue : FieldValue<BoolFieldDefinition>
{
    public bool? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is BoolFieldValue s) Value = s.Value; }
    public override string ToString() => Value.HasValue ? (Value.Value ? "Yes" : "No") : "";
}
