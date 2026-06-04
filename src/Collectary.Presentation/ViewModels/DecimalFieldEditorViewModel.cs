using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class DecimalFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly DecimalFieldDefinition _definition;
    private readonly DecimalFieldValue _fieldValue;

    [ObservableProperty]
    public partial decimal? Number { get; set; }

    public DecimalFieldEditorViewModel(DecimalFieldDefinition definition, DecimalFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Number = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Value = Number;
        return _fieldValue;
    }
}
