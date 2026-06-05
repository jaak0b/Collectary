using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class MeasurementFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly MeasurementFieldDefinition _definition;
    private readonly MeasurementFieldValue _value;

    public IReadOnlyList<string> Units { get; } = ["mm", "cm", "m", "in", "ft"];

    [ObservableProperty]
    public partial decimal? Amount { get; set; }

    [ObservableProperty]
    public partial string SelectedUnit { get; set; }

    public MeasurementFieldEditorViewModel(MeasurementFieldDefinition definition, MeasurementFieldValue value)
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
