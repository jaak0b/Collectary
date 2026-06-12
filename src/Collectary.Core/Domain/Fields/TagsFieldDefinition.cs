using Collectary.Core.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Tags")]
[FieldIcon(IconGlyphs.Bookmark)]
[FieldCatalog(2, FieldCategory.Visual)]
public class TagsFieldDefinition : FieldDefinition<TagsFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        value = new TagsFieldValue { FieldDefinitionId = Id, Tags = parts.ToList() };
        return true;
    }

    private StringListFieldSearch<TagsFieldValue> Search => new(v => v.Tags);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
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
