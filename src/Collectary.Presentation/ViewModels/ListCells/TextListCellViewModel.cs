using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.ListCells;

public class TextListCellViewModel : ListCellViewModelBase
{
    public string Text { get; }

    public TextListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        Text = source.ToString() ?? "";
    }
}
