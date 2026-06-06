using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class PercentageFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly PercentageFieldDefinition _definition;
    private readonly PercentageFieldValue _value;

    [ObservableProperty]
    public partial decimal? Number { get; set; }

    public PercentageFieldEditorViewModel(PercentageFieldDefinition definition, PercentageFieldValue value)
    {
        _definition = definition;
        _value = value;
        Number = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) => Number = data.Decimal(0m, 100m, 1);

    public override FieldValue GetCurrentValue()
    {
        _value.Value = Number;
        return _value;
    }
}
