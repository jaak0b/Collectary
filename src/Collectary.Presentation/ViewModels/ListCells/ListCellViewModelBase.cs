using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.ListCells;

public abstract class ListCellViewModelBase : ViewModelBase
{
    protected ListCellViewModelBase(FieldValue source, FieldDefinition definition)
    {
        Source = source;
        Definition = definition;
    }

    protected FieldValue Source { get; }
    protected FieldDefinition Definition { get; }
}
