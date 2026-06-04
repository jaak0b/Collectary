using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using MapsterMapper;

namespace Collectary.Presentation.ViewModels.Mapping;

public class FieldEditorMapper : IFieldEditorMapper
{
    private readonly IMapper _mapper;

    public FieldEditorMapper(IMapper mapper) => _mapper = mapper;

    public FieldDefinition ToDefinition(FieldDefinitionRowViewModel row)
    {
        var definition = row.Definition;
        if (row.IsSystemField) return definition;

        var preservedLabel = definition.Label;
        _mapper.Map(row, definition, typeof(FieldDefinitionRowViewModel), definition.GetType());
        if (definition.IsTitleField) definition.Label = preservedLabel;
        definition.GroupId = definition.IsTitleField ? null : row.AssignedGroupId;

        switch (definition)
        {
            case SingleChoiceFieldDefinition sc:
                sc.Choices = BuildChoices(row);
                break;
            case MultiChoiceFieldDefinition mc:
                mc.Choices = BuildChoices(row);
                break;
            case ListFieldDefinition lfd:
                BuildList(row, lfd);
                break;
        }

        return definition;
    }

    public FieldGroup ToGroup(FieldGroupRowViewModel groupRow, Guid? presetId, Guid? parentListFieldDefinitionId)
    {
        var group = _mapper.Map<FieldGroupRowViewModel, FieldGroup>(groupRow);
        group.Name = group.Name.Trim();
        group.PresetId = presetId;
        group.ParentListFieldDefinitionId = parentListFieldDefinitionId;
        return group;
    }

    private List<ChoiceOption> BuildChoices(FieldDefinitionRowViewModel row) =>
        row.ChoiceItems
            .Select((item, index) => new ChoiceOption { Value = item.Value, DisplayOrder = index })
            .ToList();

    private void BuildList(FieldDefinitionRowViewModel row, ListFieldDefinition list)
    {
        var flat = new EditorNodeTreeBuilder().Flatten(row.SubFieldRows);
        list.Groups = flat.Groups
            .Select(g => ToGroup(g, presetId: null, parentListFieldDefinitionId: list.Id))
            .ToList();
        list.SubFields = flat.Fields
            .Select(subRow =>
            {
                var sub = ToDefinition(subRow);
                sub.DisplayOrder = subRow.DisplayOrder;
                sub.ParentListFieldDefinitionId = list.Id;
                return sub;
            })
            .ToList();
    }
}
