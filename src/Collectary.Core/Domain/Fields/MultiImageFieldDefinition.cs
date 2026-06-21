namespace Collectary.Core.Domain.Fields;

/// <summary>One picture: the blob-store <paramref name="Key"/> and the original <paramref name="FileName"/>.</summary>
public sealed record MultiImagePicture(string Key, string FileName);

/// <summary>Holds several images per item (front/back, condition shots, multiple angles), in order.</summary>
[LocalizedName("FieldType_MultiImage")]
[FieldIcon(IconGlyphs.ImageMultiple)]
[FieldCatalog(1, FieldCategory.MediaAndFiles)]
public class MultiImageFieldDefinition : FieldDefinition<MultiImageFieldValue>
{
    public override int DefaultColumnSpan => 2;
}

public class MultiImageFieldValue : FieldValue<MultiImageFieldDefinition>
{
    public List<MultiImagePicture> Pictures { get; set; } = new();

    /// <summary>
    /// Legacy wire/storage shape. Documents already synced to users' clouds carry an "ImageKeys"
    /// array instead of "Pictures"; the getter keeps emitting it so older clients still resolve the
    /// blobs, and the setter rebuilds <see cref="Pictures"/> using the key as the display name when no
    /// richer Pictures data was supplied (the user can rename it afterwards).
    /// </summary>
    public List<string> ImageKeys
    {
        get => Pictures.Select(p => p.Key).ToList();
        set
        {
            if (Pictures.Count > 0) return;
            Pictures = value.Select(key => new MultiImagePicture(key, key)).ToList();
        }
    }

    public override bool IsEmpty => Pictures.Count == 0;

    public override void CopyFrom(FieldValue source)
    {
        if (source is MultiImageFieldValue s) Pictures = new List<MultiImagePicture>(s.Pictures);
    }

    public override IEnumerable<string> ReferencedBlobKeys() =>
        Pictures.Select(p => p.Key).Where(k => !string.IsNullOrEmpty(k));

    public override string ToString() => Pictures.Count.ToString();
}
