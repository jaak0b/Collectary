namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Decimal")]
[FieldIcon(IconGlyphs.NumberSymbol)]
[FieldCatalog(3, FieldCategory.TextAndNumbers)]
public class DecimalFieldDefinition : FieldDefinition<DecimalFieldValue>, IListDisplayable
{
    public int DecimalPlaces { get; set; } = 2;
    public bool ShowInList { get; set; }
}

public class DecimalFieldValue : FieldValue<DecimalFieldDefinition>
{
    public decimal? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is DecimalFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString() ?? "";
}
