namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_MultiChoice")]
[FieldIcon("☰")]
public class MultiChoiceFieldDefinition : FieldDefinition<MultiChoiceFieldValue>, IListDisplayable
{
    public List<ChoiceOption> Choices { get; set; } = new();
    public bool ShowInList { get; set; }
}

public class MultiChoiceFieldValue : FieldValue<MultiChoiceFieldDefinition>
{
    public List<string> Selected { get; set; } = new();
    public override bool IsEmpty => Selected.Count == 0;
    public override void CopyFrom(FieldValue source) { if (source is MultiChoiceFieldValue s) Selected = new List<string>(s.Selected); }
    public override string ToString() => string.Join(", ", Selected);
}
