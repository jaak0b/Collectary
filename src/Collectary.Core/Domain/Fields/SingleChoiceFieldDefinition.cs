namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_SingleChoice")]
[FieldIcon("◉")]
public class SingleChoiceFieldDefinition : FieldDefinition<SingleChoiceFieldValue>, IListDisplayable
{
    public List<ChoiceOption> Choices { get; set; } = new();
    public bool ShowInList { get; set; }
}

public class SingleChoiceFieldValue : FieldValue<SingleChoiceFieldDefinition>
{
    public string? Selected { get; set; }
    public override bool IsEmpty => string.IsNullOrEmpty(Selected);
    public override void CopyFrom(FieldValue source) { if (source is SingleChoiceFieldValue s) Selected = s.Selected; }
    public override string ToString() => Selected ?? "";
}
