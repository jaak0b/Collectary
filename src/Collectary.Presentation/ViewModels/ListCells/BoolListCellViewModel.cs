using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels.ListCells;

public class BoolListCellViewModel : ListCellViewModelBase
{
    public string Display { get; }

    public BoolListCellViewModel(FieldValue source, FieldDefinition definition) : base(source, definition)
    {
        Display = (source as BoolFieldValue)?.Value switch
        {
            true => LocalizationService.Instance["Bool_Yes"],
            false => LocalizationService.Instance["Bool_No"],
            null => "",
        };
    }
}
