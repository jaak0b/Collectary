namespace Collectary.Core.Domain.Fields;

/// <summary>A short recorded audio note — spoken tasting notes, condition narration, a sound sample.</summary>
[LocalizedName("FieldType_Audio")]
[FieldIcon("🎙")]
[FieldCatalog(7, FieldCategory.Visual)]
public class AudioFieldDefinition : FieldDefinition<AudioFieldValue>
{
    public override int DefaultColumnSpan => 2;
}

public class AudioFieldValue : FieldValue<AudioFieldDefinition>
{
    public string? AudioKey { get; set; }
    public int? DurationSeconds { get; set; }

    public override bool IsEmpty => string.IsNullOrEmpty(AudioKey);

    public override void CopyFrom(FieldValue source)
    {
        if (source is AudioFieldValue s)
        {
            AudioKey = s.AudioKey;
            DurationSeconds = s.DurationSeconds;
        }
    }

    public override string ToString() => IsEmpty ? "" : $"{DurationSeconds ?? 0}s";
}
