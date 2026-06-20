using System.Globalization;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Time")]
[FieldIcon(IconGlyphs.Clock)]
[FieldCatalog(2, FieldCategory.DateTime)]
public class TimeFieldDefinition : FieldDefinition<TimeFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 60;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (string.IsNullOrWhiteSpace(raw) || !TimeSpan.TryParse(raw, culture, out _)) return false;
        value = new TimeFieldValue { FieldDefinitionId = Id, Value = raw.Trim() };
        return true;
    }

    private StringFieldSearch<TimeFieldValue> Search => new(v => v.Value, v => v.Value);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class TimeFieldValue : FieldValue<TimeFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is TimeFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
