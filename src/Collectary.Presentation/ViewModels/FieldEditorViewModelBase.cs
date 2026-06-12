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

    /// <summary>Completes once the editor has finished any async initialisation (e.g. computing an auto-number). Default: already ready.</summary>
    public virtual Task Ready => Task.CompletedTask;

    /// <summary>Returns a user-facing error that blocks saving the item, or null when the field is valid.</summary>
    public virtual string? Validate() => null;
}
