namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Duration")]
[FieldIcon(IconGlyphs.Timer)]
[FieldCatalog(8, FieldCategory.TextAndNumbers)]
public class DurationFieldDefinition : FieldDefinition<DurationFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
}

public class DurationFieldValue : FieldValue<DurationFieldDefinition>
{
    public int? TotalMinutes { get; set; }
    public override bool IsEmpty => TotalMinutes is null;
    public override void CopyFrom(FieldValue source) { if (source is DurationFieldValue s) TotalMinutes = s.TotalMinutes; }
    public override string ToString()
    {
        if (TotalMinutes is null) return "";
        var h = TotalMinutes.Value / 60;
        var m = TotalMinutes.Value % 60;
        return h > 0 ? $"{h} h {m:D2} min" : $"{m} min";
    }
}
