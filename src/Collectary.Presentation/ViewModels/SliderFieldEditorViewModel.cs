using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class SliderFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly SliderFieldDefinition _definition;
    private readonly SliderFieldValue _value;

    public double Minimum => 0;
    public double Maximum => 100;

    [ObservableProperty]
    public partial double Number { get; set; }

    public SliderFieldEditorViewModel(SliderFieldDefinition definition, SliderFieldValue value)
    {
        _definition = definition;
        _value = value;
        Number = value.Value ?? 0;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Value = (int)Math.Round(Number);
        return _value;
    }
}
