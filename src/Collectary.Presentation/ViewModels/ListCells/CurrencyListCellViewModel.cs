using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels.ListCells;

public class CurrencyListCellViewModel : ListCellViewModelBase
{
    public string Display { get; }

    public CurrencyListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        var symbol = (definition as CurrencyFieldDefinition)?.CurrencySymbol ?? "";
        Display = source is CurrencyFieldValue { Value: { } v } ? $"{symbol} {v:F2}" : "";
    }
}
