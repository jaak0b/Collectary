namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Url")]
[FieldIcon("🔗")]
public class UrlFieldDefinition : FieldDefinition<UrlFieldValue>, IListDisplayable
{
    public UrlFieldDefinition() => ColumnSpan = 2;
    public bool ShowInList { get; set; }
}

public class UrlFieldValue : FieldValue<UrlFieldDefinition>
{
    public string? Url { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Url);
    public override void CopyFrom(FieldValue source) { if (source is UrlFieldValue s) Url = s.Url; }
    public override string ToString() => Url ?? "";
}
