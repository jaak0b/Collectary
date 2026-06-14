using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Url")]
[FieldIcon(IconGlyphs.Link)]
[FieldCatalog(10, FieldCategory.TextAndNumbers)]
public class UrlFieldDefinition : FieldDefinition<UrlFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 130;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        var hasScheme = Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFtp);
        if (!hasScheme && !text.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) return false;
        value = new UrlFieldValue { FieldDefinitionId = Id, Url = hasScheme ? text : $"https://{text}" };
        return true;
    }

    private StringFieldSearch<UrlFieldValue> Search => new(v => v.Url, v => v.Url);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class UrlFieldValue : FieldValue<UrlFieldDefinition>
{
    public string? Url { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Url);
    public override void CopyFrom(FieldValue source) { if (source is UrlFieldValue s) Url = s.Url; }
    public override string ToString() => Url ?? "";
}
