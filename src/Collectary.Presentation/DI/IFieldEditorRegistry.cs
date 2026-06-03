using Collectary.Core.Domain;
using Collectary.UI.ViewModels;

namespace Collectary.UI.DI;

public interface IFieldEditorRegistry
{
    FieldEditorViewModelBase? Create(FieldDefinition definition, FieldValue? existing, ItemEditingContext context);
}
