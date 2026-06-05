using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class WeightFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly WeightFieldDefinition _definition;
    private readonly WeightFieldValue _value;

    public IReadOnlyList<string> Units { get; } = ["g", "oz", "kg", "lb"];

    [ObservableProperty]
    public partial decimal? Amount { get; set; }

    [ObservableProperty]
    public partial string SelectedUnit { get; set; }

    public WeightFieldEditorViewModel(WeightFieldDefinition definition, WeightFieldValue value)
    {
        _definition = definition;
        _value = value;
        Amount = value.Amount;
        SelectedUnit = value.Unit;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Amount = Amount;
        _value.Unit = SelectedUnit;
        return _value;
    }
}
