namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Color")]
[FieldIcon("🎨")]
public class ColorFieldDefinition : FieldDefinition<ColorFieldValue>, IListDisplayable
{
    public ColorFieldDefinition() => ColumnSpan = 2;
    public ColorFormat Format { get; set; } = ColorFormat.Hex;
    public bool ShowInList { get; set; }
}

public class ColorFieldValue : FieldValue<ColorFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrEmpty(Value);
    public override void CopyFrom(FieldValue source) { if (source is ColorFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
