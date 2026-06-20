using System.Text.RegularExpressions;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Phone")]
[FieldIcon(IconGlyphs.Call)]
[FieldCatalog(3, FieldCategory.Text)]
public class PhoneFieldDefinition : FieldDefinition<PhoneFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 140;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        if (text.Count(char.IsDigit) < 5 || !Regex.IsMatch(text, @"^\+?[0-9\s\-()./]+$")) return false;
        value = new PhoneFieldValue { FieldDefinitionId = Id, Value = text };
        return true;
    }

    private StringFieldSearch<PhoneFieldValue> Search => new(v => v.Value, v => v.Value);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class PhoneFieldValue : FieldValue<PhoneFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is PhoneFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
