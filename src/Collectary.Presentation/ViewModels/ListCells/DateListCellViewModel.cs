using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels.ListCells;

public class DateListCellViewModel : ListCellViewModelBase
{
    public string Text { get; }

    public DateListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        var value = (source as DateFieldValue)?.Value;
        var withTime = (definition as DateFieldDefinition)?.WithTime ?? false;
        Text = value?.ToString(withTime ? "g" : "d", CultureInfo.CurrentCulture) ?? "";
    }
}
