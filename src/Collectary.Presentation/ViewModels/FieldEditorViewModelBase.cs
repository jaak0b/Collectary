using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels;

public abstract class FieldEditorViewModelBase : ViewModelBase
{
    public abstract FieldDefinition Definition { get; }
    public int ColumnSpan => Definition.ColumnSpan;
    public abstract FieldValue GetCurrentValue();
}
