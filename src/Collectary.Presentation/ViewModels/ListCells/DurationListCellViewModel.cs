using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.ListCells;

public class DurationListCellViewModel : ListCellViewModelBase
{
    public string Display { get; }

    public DurationListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        Display = source.ToString() ?? "";
    }
}
