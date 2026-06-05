namespace Collectary.Core.Domain.Fields;

/// <summary>Links this item to another item (a minifig to its set, a lens to its body, a card to its deck).</summary>
[LocalizedName("FieldType_LinkedItem")]
[FieldIcon(IconGlyphs.LinkMultiple)]
[FieldCatalog(3, FieldCategory.Choice)]
public class LinkedItemFieldDefinition : FieldDefinition<LinkedItemFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }
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
