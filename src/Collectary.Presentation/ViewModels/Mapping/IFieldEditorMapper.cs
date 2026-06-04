using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.Mapping;

public interface IFieldEditorMapper
{
    FieldDefinition ToDefinition(FieldDefinitionRowViewModel row);
    FieldGroup ToGroup(FieldGroupRowViewModel groupRow, Guid? presetId, Guid? parentListFieldDefinitionId);
}
