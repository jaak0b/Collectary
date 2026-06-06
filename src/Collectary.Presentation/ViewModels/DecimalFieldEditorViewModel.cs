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

    public string FormatString => _definition.DecimalPlaces > 0
        ? "0." + new string('0', _definition.DecimalPlaces)
        : "0";

    public DecimalFieldEditorViewModel(DecimalFieldDefinition definition, DecimalFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Number = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) =>
        Number = data.Decimal(1m, 1000m, _definition.DecimalPlaces);

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Value = Number;
        return _fieldValue;
    }
}
