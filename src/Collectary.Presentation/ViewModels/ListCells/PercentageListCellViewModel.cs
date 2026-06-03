using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels.ListCells;

public class PercentageListCellViewModel : ListCellViewModelBase
{
    public string Display { get; }

    public PercentageListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        Display = source is PercentageFieldValue { Value: { } v } ? $"{v:F1} %" : "";
    }
}
