using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class IntegerFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly IntegerFieldDefinition _definition;
    private readonly IntegerFieldValue _fieldValue;

    [ObservableProperty]
    public partial int? Number { get; set; }

    public decimal Minimum => _definition.Min ?? int.MinValue;
    public decimal Maximum => _definition.Max ?? int.MaxValue;

    public IntegerFieldEditorViewModel(IntegerFieldDefinition definition, IntegerFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Number = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) =>
        Number = data.Int(_definition.Min ?? 1, _definition.Max ?? 1000);

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Value = Number;
        return _fieldValue;
    }
}
