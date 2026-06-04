namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_SingleChoice")]
[FieldIcon("◉")]
[FieldCatalog(1, FieldCategory.Choice)]
public class SingleChoiceFieldDefinition : FieldDefinition<SingleChoiceFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public List<ChoiceOption> Choices { get; set; } = new();
    public bool ShowInList { get; set; }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is not SingleChoiceFieldDefinition src) return;
        Choices.Clear();
        foreach (var c in src.Choices)
            Choices.Add(new ChoiceOption { Id = c.Id, Value = c.Value, DisplayOrder = c.DisplayOrder });
    }
}

public class SingleChoiceFieldValue : FieldValue<SingleChoiceFieldDefinition>
{
    public string? Selected { get; set; }
    public override bool IsEmpty => string.IsNullOrEmpty(Selected);
    public override void CopyFrom(FieldValue source) { if (source is SingleChoiceFieldValue s) Selected = s.Selected; }
    public override string ToString() => Selected ?? "";
}
