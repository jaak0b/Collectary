using Collectary.Core.Domain;

namespace Collectary.UI.ViewModels;

public abstract class FieldEditorViewModelBase : ViewModelBase
{
    public abstract FieldDefinition Definition { get; }
    public abstract FieldValue GetCurrentValue();
}
