using Collectary.Core.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_SingleChoice")]
[FieldIcon(IconGlyphs.RadioButton)]
[FieldCatalog(1, FieldCategory.Choice)]
public class SingleChoiceFieldDefinition : FieldDefinition<SingleChoiceFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public List<ChoiceOption> Choices { get; set; } = new();
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var text = raw.Trim();
        if (Choices.Count > 0 && !Choices.Any(c => string.Equals(c.Value, text, StringComparison.OrdinalIgnoreCase)))
            return false;
        value = new SingleChoiceFieldValue { FieldDefinitionId = Id, Selected = text };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is not SingleChoiceFieldDefinition src) return;
        Choices.Clear();
        foreach (var c in src.Choices)
            Choices.Add(new ChoiceOption { Id = c.Id, Value = c.Value, DisplayOrder = c.DisplayOrder });
    }

    private StringFieldSearch<SingleChoiceFieldValue> Search => new(v => v.Selected, v => v.Selected);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() =>
        Choices.OrderBy(c => c.DisplayOrder).Select(c => c.Value);

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class SingleChoiceFieldValue : FieldValue<SingleChoiceFieldDefinition>
{
    public string? Selected { get; set; }
    public override bool IsEmpty => string.IsNullOrEmpty(Selected);
    public override void CopyFrom(FieldValue source) { if (source is SingleChoiceFieldValue s) Selected = s.Selected; }
    public override string ToString() => Selected ?? "";
}
