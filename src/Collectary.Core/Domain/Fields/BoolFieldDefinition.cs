using System.Globalization;
using Collectary.Core.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Bool")]
[FieldIcon(IconGlyphs.Checkbox)]
[FieldCatalog(0, FieldCategory.Choice)]
public class BoolFieldDefinition : FieldDefinition<BoolFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public bool ShowInList { get; set; }
    public bool ThreeState { get; set; }

    public int ImportInferenceOrder => 310;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var token = raw.Trim().ToLowerInvariant();
        if (token is "true" or "yes" or "y" or "1" or "x" or "✓" or "ja" or "wahr")
        {
            value = new BoolFieldValue { FieldDefinitionId = Id, Value = true };
            return true;
        }
        if (token is "false" or "no" or "n" or "0" or "nein" or "falsch")
        {
            value = new BoolFieldValue { FieldDefinitionId = Id, Value = false };
            return true;
        }
        return false;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is BoolFieldDefinition src) ThreeState = src.ThreeState;
    }

    private ComparableFieldSearch<BoolFieldValue, bool> Search => new(
        v => v.Value, v => v.Value,
        raw => TryImportFromText(raw, CultureInfo.InvariantCulture, out var parsed)
            && parsed is BoolFieldValue flag
            ? flag.Value
            : null,
        ordered: false);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => ["true", "false"];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class BoolFieldValue : FieldValue<BoolFieldDefinition>
{
    public bool? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is BoolFieldValue s) Value = s.Value; }
    public override string ToString() => Value.HasValue ? (Value.Value ? "Yes" : "No") : "";
}
