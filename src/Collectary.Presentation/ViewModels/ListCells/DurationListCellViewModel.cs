using Collectary.Core.Domain;

namespace Collectary.UI.ViewModels.ListCells;

public class DurationListCellViewModel : ListCellViewModelBase
{
    public string Display { get; }

    public DurationListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        Display = source.ToString() ?? "";
    }
}
