using System.Globalization;
using System.Text.RegularExpressions;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Duration")]
[FieldIcon(IconGlyphs.Timer)]
[FieldCatalog(3, FieldCategory.DateTime)]
public class DurationFieldDefinition : FieldDefinition<DurationFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 340;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        if (text.Length == 0) return false;

        int totalMinutes;
        if (int.TryParse(text, NumberStyles.Integer, culture, out var plainMinutes))
        {
            if (plainMinutes < 0) return false;
            totalMinutes = plainMinutes;
        }
        else if (text.Contains(':'))
        {
            var parts = text.Split(':');
            if (parts.Length is < 2 or > 3) return false;
            if (!int.TryParse(parts[0], NumberStyles.Integer, culture, out var hours) || hours < 0) return false;
            if (!int.TryParse(parts[1], NumberStyles.Integer, culture, out var minutes) || minutes is < 0 or > 59) return false;
            if (parts.Length == 3 && (!int.TryParse(parts[2], NumberStyles.Integer, culture, out var seconds) || seconds is < 0 or > 59)) return false;
            totalMinutes = hours * 60 + minutes;
        }
        else
        {
            var match = Regex.Match(text, @"^\s*(?:(\d+)\s*h(?:ours?|rs?)?)?\s*(?:(\d+)\s*m(?:in(?:utes?)?)?)?\s*$", RegexOptions.IgnoreCase);
            if (!match.Success || (!match.Groups[1].Success && !match.Groups[2].Success)) return false;
            var h = 0;
            var m = 0;
            if (match.Groups[1].Success && !int.TryParse(match.Groups[1].Value, NumberStyles.Integer, culture, out h)) return false;
            if (match.Groups[2].Success && !int.TryParse(match.Groups[2].Value, NumberStyles.Integer, culture, out m)) return false;
            totalMinutes = h * 60 + m;
        }

        value = new DurationFieldValue { FieldDefinitionId = Id, TotalMinutes = totalMinutes };
        return true;
    }

    private ComparableFieldSearch<DurationFieldValue, int> Search => new(
        v => v.TotalMinutes, v => v.TotalMinutes,
        raw => TryImportFromText(raw, CultureInfo.InvariantCulture, out var parsed)
            && parsed is DurationFieldValue duration
            ? duration.TotalMinutes
            : null);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class DurationFieldValue : FieldValue<DurationFieldDefinition>
{
    public int? TotalMinutes { get; set; }
    public override bool IsEmpty => TotalMinutes is null;
    public override void CopyFrom(FieldValue source) { if (source is DurationFieldValue s) TotalMinutes = s.TotalMinutes; }
    public override string ToString()
    {
        if (TotalMinutes is null) return "";
        var h = TotalMinutes.Value / 60;
        var m = TotalMinutes.Value % 60;
        return h > 0 ? $"{h} h {m:D2} min" : $"{m} min";
    }
}
