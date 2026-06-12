using System.Globalization;
using Collectary.Core.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Decimal")]
[FieldIcon(IconGlyphs.NumberSymbol)]
[FieldCatalog(3, FieldCategory.TextAndNumbers)]
public class DecimalFieldDefinition : FieldDefinition<DecimalFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public int DecimalPlaces { get; set; } = 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 40;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!decimal.TryParse(raw, NumberStyles.Number, culture, out var d)) return false;
        value = new DecimalFieldValue { FieldDefinitionId = Id, Value = d };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is DecimalFieldDefinition src) DecimalPlaces = src.DecimalPlaces;
    }

    private ComparableFieldSearch<DecimalFieldValue, decimal> Search => new(
        v => v.Value, v => v.Value,
        raw => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class DecimalFieldValue : FieldValue<DecimalFieldDefinition>
{
    public decimal? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is DecimalFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString() ?? "";
}
