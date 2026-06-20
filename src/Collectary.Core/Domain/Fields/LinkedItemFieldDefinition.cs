using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

/// <summary>Links this item to another item (a minifig to its set, a lens to its body, a card to its deck).</summary>
[LocalizedName("FieldType_LinkedItem")]
[FieldIcon(IconGlyphs.LinkMultiple)]
[FieldCatalog(5, FieldCategory.Choice)]
public class LinkedItemFieldDefinition : FieldDefinition<LinkedItemFieldValue>, IListDisplayable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    private StringFieldSearch<LinkedItemFieldValue> Search => new(v => v.TargetDisplay, v => v.TargetDisplay);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class LinkedItemFieldValue : FieldValue<LinkedItemFieldDefinition>
{
    public Guid? TargetItemId { get; set; }

    /// <summary>A cached label for the target so the link renders without a join.</summary>
    public string? TargetDisplay { get; set; }

    public override bool IsEmpty => TargetItemId is null;

    public override void CopyFrom(FieldValue source)
    {
        if (source is LinkedItemFieldValue s)
        {
            TargetItemId = s.TargetItemId;
            TargetDisplay = s.TargetDisplay;
        }
    }

    public override string ToString() => TargetDisplay ?? "";
}
