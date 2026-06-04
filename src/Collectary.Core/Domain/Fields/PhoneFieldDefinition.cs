namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Phone")]
[FieldIcon("📞")]
public class PhoneFieldDefinition : FieldDefinition<PhoneFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }
}

public class PhoneFieldValue : FieldValue<PhoneFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is PhoneFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
