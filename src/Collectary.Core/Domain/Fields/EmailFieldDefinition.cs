using System.Text.RegularExpressions;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Email")]
[FieldIcon(IconGlyphs.Mail)]
[FieldCatalog(4, FieldCategory.Text)]
public class EmailFieldDefinition : FieldDefinition<EmailFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 120;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        if (!Regex.IsMatch(text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return false;
        value = new EmailFieldValue { FieldDefinitionId = Id, Value = text };
        return true;
    }

    private StringFieldSearch<EmailFieldValue> Search => new(v => v.Value, v => v.Value);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class EmailFieldValue : FieldValue<EmailFieldDefinition>
{
    public string? Value { get; set; }
    public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override void CopyFrom(FieldValue source) { if (source is EmailFieldValue s) Value = s.Value; }
    public override string ToString() => Value ?? "";
}
