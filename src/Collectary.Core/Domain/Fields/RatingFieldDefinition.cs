using System.Globalization;
using Collectary.Core.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Rating")]
[FieldIcon(IconGlyphs.Star)]
[FieldCatalog(1, FieldCategory.Visual)]
public class RatingFieldDefinition : FieldDefinition<RatingFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public int MaxStars { get; set; } = 5;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 200;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!int.TryParse(raw, NumberStyles.Integer, culture, out var stars) || stars < 0 || stars > MaxStars) return false;
        value = new RatingFieldValue { FieldDefinitionId = Id, Stars = stars };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is RatingFieldDefinition src) MaxStars = src.MaxStars;
    }

    private ComparableFieldSearch<RatingFieldValue, int> Search => new(
        v => v.Stars, v => v.Stars,
        raw => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class RatingFieldValue : FieldValue<RatingFieldDefinition>
{
    public int? Stars { get; set; }
    public override bool IsEmpty => Stars is null;
    public override void CopyFrom(FieldValue source) { if (source is RatingFieldValue s) Stars = s.Stars; }
    public override string ToString() => Stars.HasValue ? new string('★', Stars.Value) : "";
}
