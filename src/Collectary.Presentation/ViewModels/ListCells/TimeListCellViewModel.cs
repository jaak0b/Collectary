using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.ListCells;

public class TimeListCellViewModel : ListCellViewModelBase
{
    public string Display { get; }

    public TimeListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        Display = source.ToString() ?? "";
    }
}
