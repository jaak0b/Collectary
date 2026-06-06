namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Integer")]
[FieldIcon(IconGlyphs.NumberSymbol)]
[FieldCatalog(2, FieldCategory.TextAndNumbers)]
public class IntegerFieldDefinition : FieldDefinition<IntegerFieldValue>, IListDisplayable
{
    public int? Min { get; set; }
    public int? Max { get; set; }
    public bool ShowInList { get; set; }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is not IntegerFieldDefinition src) return;
        Min = src.Min;
        Max = src.Max;
    }
}

public class IntegerFieldValue : FieldValue<IntegerFieldDefinition>
{
    public int? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is IntegerFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString() ?? "";
}
