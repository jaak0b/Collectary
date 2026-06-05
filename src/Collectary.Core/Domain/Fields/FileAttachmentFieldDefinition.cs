namespace Collectary.Core.Domain.Fields;

/// <summary>One stored file: the blob-store <paramref name="Key"/> and the original <paramref name="FileName"/>.</summary>
public sealed record FileAttachment(string Key, string FileName);

/// <summary>Attaches documents to an item — manuals, warranties, certificates, receipts, instructions.</summary>
[LocalizedName("FieldType_FileAttachment")]
[FieldIcon("📎")]
[FieldCatalog(13, FieldCategory.TextAndNumbers)]
public class FileAttachmentFieldDefinition : FieldDefinition<FileAttachmentFieldValue>
{
    public override int DefaultColumnSpan => 2;
}

public class FileAttachmentFieldValue : FieldValue<FileAttachmentFieldDefinition>
{
    public List<FileAttachment> Files { get; set; } = new();

    public override bool IsEmpty => Files.Count == 0;

    public override void CopyFrom(FieldValue source)
    {
        if (source is FileAttachmentFieldValue s) Files = new List<FileAttachment>(s.Files);
    }

    public override string ToString() => Files.Count.ToString();
}
