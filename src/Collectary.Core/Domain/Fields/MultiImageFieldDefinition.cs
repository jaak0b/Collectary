namespace Collectary.Core.Domain.Fields;

/// <summary>Holds several images per item (front/back, condition shots, multiple angles), in order.</summary>
[LocalizedName("FieldType_MultiImage")]
[FieldIcon(IconGlyphs.ImageMultiple)]
[FieldCatalog(4, FieldCategory.Visual)]
public class MultiImageFieldDefinition : FieldDefinition<MultiImageFieldValue>
{
    public override int DefaultColumnSpan => 2;
}

public class MultiImageFieldValue : FieldValue<MultiImageFieldDefinition>
{
    public List<string> ImageKeys { get; set; } = new();

    public override bool IsEmpty => ImageKeys.Count == 0;

    public override void CopyFrom(FieldValue source)
    {
        if (source is MultiImageFieldValue s) ImageKeys = new List<string>(s.ImageKeys);
    }

    public override IEnumerable<string> ReferencedBlobKeys() => ImageKeys.Where(k => !string.IsNullOrEmpty(k));

    public override string ToString() => ImageKeys.Count.ToString();
}
