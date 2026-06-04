using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.ListCells;

public class TagsListCellViewModel : ListCellViewModelBase
{
    public string Display { get; }

    public TagsListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        Display = source.ToString() ?? "";
    }
}
