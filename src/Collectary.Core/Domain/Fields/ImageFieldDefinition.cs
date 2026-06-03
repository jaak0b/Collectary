namespace Collectary.Core.Domain.Fields;

public enum ImageSizeMode { Fixed, Min, Max }

[LocalizedName("FieldType_Image")]
[FieldIcon("🖼")]
public class ImageFieldDefinition : FieldDefinition<ImageFieldValue>
{
    public ImageFieldDefinition() => ColumnSpan = 2;
    public int DisplayWidth { get; set; } = 200;
    public int DisplayHeight { get; set; } = 200;
    public ImageSizeMode SizeMode { get; set; } = ImageSizeMode.Fixed;
}

public class ImageFieldValue : FieldValue<ImageFieldDefinition>
{
    public string? ImageKey { get; set; }
    public string? FileName { get; set; }
    public override bool IsEmpty => string.IsNullOrEmpty(ImageKey);
    public override void CopyFrom(FieldValue source)
    {
        if (source is ImageFieldValue s) { ImageKey = s.ImageKey; FileName = s.FileName; }
    }
}
