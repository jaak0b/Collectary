using System.Globalization;
using Collectary.Core.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Integer")]
[FieldIcon(IconGlyphs.NumberSymbol)]
[FieldCatalog(2, FieldCategory.TextAndNumbers)]
public class IntegerFieldDefinition : FieldDefinition<IntegerFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public int? Min { get; set; }
    public int? Max { get; set; }
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 20;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!int.TryParse(raw, NumberStyles.Integer | NumberStyles.AllowThousands, culture, out var n)) return false;
        value = new IntegerFieldValue { FieldDefinitionId = Id, Value = n };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is not IntegerFieldDefinition src) return;
        Min = src.Min;
        Max = src.Max;
    }

    private ComparableFieldSearch<IntegerFieldValue, int> Search => new(
        v => v.Value, v => v.Value,
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

public class IntegerFieldValue : FieldValue<IntegerFieldDefinition>
{
    public int? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is IntegerFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString() ?? "";
}
