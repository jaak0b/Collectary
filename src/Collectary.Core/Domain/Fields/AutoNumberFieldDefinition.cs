using System.Globalization;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

public enum AutoNumberStrategy { HighestPlusOne, FillGaps }

public enum DuplicateHandling { Error, Warn, Allow }

[LocalizedName("FieldType_AutoNumber")]
[FieldIcon(IconGlyphs.NumberSymbol)]
[FieldCatalog(17, FieldCategory.TextAndNumbers)]
public class AutoNumberFieldDefinition : FieldDefinition<AutoNumberFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public bool Editable { get; set; }
    public AutoNumberStrategy Strategy { get; set; } = AutoNumberStrategy.HighestPlusOne;
    public DuplicateHandling OnDuplicate { get; set; } = DuplicateHandling.Error;
    public bool ShowInList { get; set; } = true;

    public int NextNumber(IReadOnlyCollection<int> used)
    {
        if (Strategy == AutoNumberStrategy.FillGaps) return LowestFreeNumber(used);
        var highest = used.Count == 0 ? 0 : used.Max();
        return highest == int.MaxValue ? LowestFreeNumber(used) : highest + 1;
    }

    private int LowestFreeNumber(IReadOnlyCollection<int> used) =>
        Enumerable.Range(1, used.Count + 1).First(n => !used.Contains(n));

    public bool EnforcesUniqueImportValues => OnDuplicate != DuplicateHandling.Allow;

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!int.TryParse(raw, NumberStyles.Integer | NumberStyles.AllowThousands, culture, out var n)) return false;
        value = new AutoNumberFieldValue { FieldDefinitionId = Id, Value = n };
        return true;
    }

    public void ApplyImportDefaults()
    {
        Editable = true;
        OnDuplicate = DuplicateHandling.Warn;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is not AutoNumberFieldDefinition src) return;
        Editable = src.Editable;
        Strategy = src.Strategy;
        OnDuplicate = src.OnDuplicate;
    }

    private ComparableFieldSearch<AutoNumberFieldValue, int> Search => new(
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

public class AutoNumberFieldValue : FieldValue<AutoNumberFieldDefinition>
{
    public int? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is AutoNumberFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString() ?? "";
}
