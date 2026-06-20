using System.Globalization;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Currency")]
[FieldIcon(IconGlyphs.Money)]
[FieldCatalog(4, FieldCategory.Numbers)]
public class CurrencyFieldDefinition : FieldDefinition<CurrencyFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public bool ShowInList { get; set; }
    public string CurrencySymbol { get; set; } = "€";

    public int ImportInferenceOrder => 50;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        const NumberStyles styles = NumberStyles.Currency & ~NumberStyles.AllowParentheses;
        var withoutSymbol = string.IsNullOrEmpty(CurrencySymbol) ? raw : raw.Replace(CurrencySymbol, "");
        if (!decimal.TryParse(raw, styles, culture, out var d)
            && !decimal.TryParse(withoutSymbol, styles, culture, out d))
            return false;
        value = new CurrencyFieldValue { FieldDefinitionId = Id, Value = d };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is CurrencyFieldDefinition src) CurrencySymbol = src.CurrencySymbol;
    }

    private ComparableFieldSearch<CurrencyFieldValue, decimal> Search => new(
        v => v.Value, v => v.Value,
        raw =>
        {
            var withoutSymbol = string.IsNullOrEmpty(CurrencySymbol) ? raw : raw.Replace(CurrencySymbol, "");
            return decimal.TryParse(
                withoutSymbol.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        });

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class CurrencyFieldValue : FieldValue<CurrencyFieldDefinition>
{
    public decimal? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is CurrencyFieldValue s) Value = s.Value; }
    public override string ToString() => Value.HasValue ? $"{Value:F2}" : "";
}
