namespace Collectary.Core.Domain.Fields;

/// <summary>A 0–100 value set with a visual slider — handy for condition, intensity, or any bounded score.</summary>
[LocalizedName("FieldType_Slider")]
[FieldIcon("🎚")]
[FieldCatalog(16, FieldCategory.TextAndNumbers)]
public class SliderFieldDefinition : FieldDefinition<SliderFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
}

public class SliderFieldValue : FieldValue<SliderFieldDefinition>
{
    public int? Value { get; set; }

    public override bool IsEmpty => Value is null;

    public override void CopyFrom(FieldValue source)
    {
        if (source is SliderFieldValue s) Value = s.Value;
    }

    public override string ToString() => Value?.ToString() ?? "";
}
