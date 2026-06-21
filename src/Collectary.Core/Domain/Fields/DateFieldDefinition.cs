using System.Globalization;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Date")]
[FieldIcon(IconGlyphs.Calendar)]
[FieldCatalog(0, FieldCategory.DateTime)]
public class DateFieldDefinition : FieldDefinition<DateFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public bool ShowInList { get; set; }

    public bool WithTime { get; set; }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is DateFieldDefinition src) WithTime = src.WithTime;
    }

    public int ImportInferenceOrder => 70;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!DateTime.TryParse(raw, culture, DateTimeStyles.None, out var dt)) return false;
        value = new DateFieldValue { FieldDefinitionId = Id, Value = dt.Date };
        return true;
    }

    private ComparableFieldSearch<DateFieldValue, DateTime> Search => new(
        v => v.Value?.Date, v => v.Value,
        raw => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class DateFieldValue : FieldValue<DateFieldDefinition>
{
    public DateTime? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is DateFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString("d") ?? "";
}
