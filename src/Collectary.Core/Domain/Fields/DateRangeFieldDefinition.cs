namespace Collectary.Core.Domain.Fields;

using System.Globalization;
using Collectary.Core.Search;

/// <summary>A from–to date pair — an ownership period, a drink-window, a manufacturing era.</summary>
[LocalizedName("FieldType_DateRange")]
[FieldIcon(IconGlyphs.DateRange)]
[FieldCatalog(7, FieldCategory.TextAndNumbers)]
public class DateRangeFieldDefinition : FieldDefinition<DateRangeFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 80;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var separators = new[] { " – ", "–", " — ", "—", " - ", " to ", " bis ", ".." };
        foreach (var sep in separators)
        {
            var idx = raw.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var left = raw[..idx].Trim();
            var right = raw[(idx + sep.Length)..].Trim();
            if (!DateTime.TryParse(left, culture, DateTimeStyles.None, out var from)
                || !DateTime.TryParse(right, culture, DateTimeStyles.None, out var to))
                continue;
            if (from.Date > to.Date) return false;
            value = new DateRangeFieldValue { FieldDefinitionId = Id, From = from.Date, To = to.Date };
            return true;
        }
        return false;
    }

    public IReadOnlyList<QueryOperatorKind> SupportedOperators =>
    [
        QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
        QueryOperatorKind.Less, QueryOperatorKind.LessOrEqual,
        QueryOperatorKind.Greater, QueryOperatorKind.GreaterOrEqual,
        QueryOperatorKind.IsEmpty, QueryOperatorKind.IsNotEmpty,
    ];

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error)
    {
        matcher = null;
        error = null;
        if (op == QueryOperatorKind.IsEmpty)
        {
            matcher = new ValueEmptinessMatcher(expectPresent: false);
            return true;
        }
        if (op == QueryOperatorKind.IsNotEmpty)
        {
            matcher = new ValueEmptinessMatcher(expectPresent: true);
            return true;
        }
        if (!SupportedOperators.Contains(op))
        {
            error = QueryErrorCode.OperatorNotSupported;
            return false;
        }
        if (!DateTime.TryParse(operands[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            error = QueryErrorCode.InvalidValue;
            return false;
        }
        var day = parsed.Date;
        matcher = op switch
        {
            QueryOperatorKind.Equals => RangeMatcher(
                v => ContainsDay(v, day),
                v => (v.From != null || v.To != null)
                    && (v.From == null || v.From <= day)
                    && (v.To == null || v.To >= day)),
            QueryOperatorKind.NotEquals => RangeMatcher(
                v => !v.IsEmpty && !ContainsDay(v, day),
                v => (v.From != null || v.To != null)
                    && ((v.From != null && v.From > day) || (v.To != null && v.To < day))),
            QueryOperatorKind.Less => RangeMatcher(
                v => v.To < day,
                v => v.To != null && v.To < day),
            QueryOperatorKind.LessOrEqual => RangeMatcher(
                v => v.To <= day,
                v => v.To != null && v.To <= day),
            QueryOperatorKind.Greater => RangeMatcher(
                v => v.From > day,
                v => v.From != null && v.From > day),
            _ => RangeMatcher(
                v => v.From >= day,
                v => v.From != null && v.From >= day),
        };
        return true;
    }

    public IComparable? SortKey(Item item, FieldValue? value) =>
        value is DateRangeFieldValue range ? range.From ?? range.To : null;

    private bool ContainsDay(DateRangeFieldValue value, DateTime day) =>
        !value.IsEmpty
        && (value.From is null || value.From <= day)
        && (value.To is null || value.To >= day);

    private TypedValueMatcher<DateRangeFieldValue> RangeMatcher(
        Func<DateRangeFieldValue, bool> predicate,
        System.Linq.Expressions.Expression<Func<DateRangeFieldValue, bool>> serverPredicate) =>
        new(predicate, serverPredicate);
}

public class DateRangeFieldValue : FieldValue<DateRangeFieldDefinition>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public override bool IsEmpty => From is null && To is null;

    public override void CopyFrom(FieldValue source)
    {
        if (source is DateRangeFieldValue s)
        {
            From = s.From;
            To = s.To;
        }
    }

    public override string ToString()
    {
        if (From is null && To is null) return "";
        return $"{Format(From)} – {Format(To)}";
    }

    private string Format(DateTime? d) =>
        d?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "?";
}
