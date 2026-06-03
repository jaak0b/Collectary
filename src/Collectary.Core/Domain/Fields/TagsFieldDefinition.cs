namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Tags")]
[FieldIcon("🔖")]
public class TagsFieldDefinition : FieldDefinition<TagsFieldValue>, IListDisplayable
{
    public TagsFieldDefinition() => ColumnSpan = 2;
    public bool ShowInList { get; set; }
}

public class TagsFieldValue : FieldValue<TagsFieldDefinition>
{
    public List<string> Tags { get; set; } = new();
    public override bool IsEmpty => Tags.Count == 0;
    public override void CopyFrom(FieldValue source) { if (source is TagsFieldValue s) Tags = s.Tags.ToList(); }
    public override string ToString()
    {
        var joined = string.Join(", ", Tags);
        return joined.Length > 80 ? joined[..80] + "…" : joined;
    }
}
