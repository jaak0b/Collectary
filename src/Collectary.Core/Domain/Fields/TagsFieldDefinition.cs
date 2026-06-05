namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Tags")]
[FieldIcon(IconGlyphs.Bookmark)]
[FieldCatalog(2, FieldCategory.Visual)]
public class TagsFieldDefinition : FieldDefinition<TagsFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
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
