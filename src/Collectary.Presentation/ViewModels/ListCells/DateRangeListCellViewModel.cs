using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels.ListCells;

public class DateRangeListCellViewModel : ListCellViewModelBase
{
    public string Text { get; }

    public DateRangeListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        var range = source as DateRangeFieldValue;
        Text = range is null || (range.From is null && range.To is null)
            ? ""
            : new DateRangeTextFormatter().Format(range.From, range.To, CultureInfo.CurrentCulture);
    }
}
