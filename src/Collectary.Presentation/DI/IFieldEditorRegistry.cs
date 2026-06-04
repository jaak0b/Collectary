using Collectary.Core.Domain;
using Collectary.Presentation.ViewModels;

namespace Collectary.Presentation.DI;

public interface IFieldEditorRegistry
{
    FieldEditorViewModelBase? Create(FieldDefinition definition, FieldValue? existing, ItemEditingContext context);
}
