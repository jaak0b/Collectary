using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels.ListCells;

public class DecimalListCellViewModel : ListCellViewModelBase
{
    public string Display { get; }

    public DecimalListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        var places = (definition as DecimalFieldDefinition)?.DecimalPlaces ?? 2;
        Display = source is DecimalFieldValue { Value: { } v }
            ? v.ToString("F" + places, CultureInfo.InvariantCulture)
            : "";
    }
}
