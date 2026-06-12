using System.Globalization;
using System.Text.RegularExpressions;
using Collectary.Core.Search;

namespace Collectary.Core.Domain.Fields;

/// <summary>A physical measurement — a number plus a length unit (diameter, case size, dimensions, scale).</summary>
[LocalizedName("FieldType_Measurement")]
[FieldIcon(IconGlyphs.Ruler)]
[FieldCatalog(15, FieldCategory.TextAndNumbers)]
public class MeasurementFieldDefinition : FieldDefinition<MeasurementFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 310;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var match = Regex.Match(raw.Trim(), @"^(?<amount>[-+]?\d[\d.,\s]*)\s*(?<unit>[^\d\s].*)$");
        if (!match.Success || !decimal.TryParse(match.Groups["amount"].Value.Trim(), NumberStyles.Number, culture, out var amount))
            return false;
        value = new MeasurementFieldValue { FieldDefinitionId = Id, Amount = amount, Unit = match.Groups["unit"].Value.Trim() };
        return true;
    }

    private ComparableFieldSearch<MeasurementFieldValue, decimal> Search => new(
        v => v.Amount, v => v.Amount,
        raw =>
        {
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var plain))
                return plain;
            return TryImportFromText(raw, CultureInfo.InvariantCulture, out var parsed)
                && parsed is MeasurementFieldValue measurement
                ? measurement.Amount
                : null;
        },
        operandConstraint: raw => UnitIn(raw) is { } unit
            ? v => string.Equals(v.Unit.Trim(), unit, StringComparison.OrdinalIgnoreCase)
            : null);

    private string? UnitIn(string raw) =>
        !decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
            && TryImportFromText(raw, CultureInfo.InvariantCulture, out var parsed)
            && parsed is MeasurementFieldValue measurement
            ? measurement.Unit
            : null;

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class MeasurementFieldValue : FieldValue<MeasurementFieldDefinition>
{
    public decimal? Amount { get; set; }
    public string Unit { get; set; } = "mm";

    public override bool IsEmpty => Amount is null;

    public override void CopyFrom(FieldValue source)
    {
        if (source is MeasurementFieldValue s)
        {
            Amount = s.Amount;
            Unit = s.Unit;
        }
    }

    public override string ToString() =>
        Amount.HasValue ? $"{Amount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Unit}" : "";
}
