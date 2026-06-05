namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_MultiChoice")]
[FieldIcon(IconGlyphs.Multiselect)]
[FieldCatalog(2, FieldCategory.Choice)]
public class MultiChoiceFieldDefinition : FieldDefinition<MultiChoiceFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public List<ChoiceOption> Choices { get; set; } = new();
    public bool ShowInList { get; set; }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is not MultiChoiceFieldDefinition src) return;
        Choices.Clear();
        foreach (var c in src.Choices)
            Choices.Add(new ChoiceOption { Id = c.Id, Value = c.Value, DisplayOrder = c.DisplayOrder });
    }
}

public class MultiChoiceFieldValue : FieldValue<MultiChoiceFieldDefinition>
{
    public List<string> Selected { get; set; } = new();
    public override bool IsEmpty => Selected.Count == 0;
    public override void CopyFrom(FieldValue source) { if (source is MultiChoiceFieldValue s) Selected = new List<string>(s.Selected); }
    public override string ToString() => string.Join(", ", Selected);
}
