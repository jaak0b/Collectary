using Collectary.Core.Domain;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public abstract class FieldEditorViewModelBase : ViewModelBase
{
    public abstract FieldDefinition Definition { get; }
    public int ColumnSpan => Definition.ColumnSpan;

    public virtual void Randomize(ISampleData data)
    {
    }

    private bool _labelAbove;
    /// <summary>When true the editor renders its label above the input instead of beside it.</summary>
    public bool LabelAbove
    {
        get => _labelAbove;
        set => SetProperty(ref _labelAbove, value);
    }

    public abstract FieldValue GetCurrentValue();
}
