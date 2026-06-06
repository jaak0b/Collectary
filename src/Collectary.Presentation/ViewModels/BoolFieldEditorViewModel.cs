using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class BoolFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly BoolFieldDefinition _definition;
    private readonly BoolFieldValue _value;

    [ObservableProperty]
    public partial bool? IsChecked { get; set; }

    public bool IsThreeState => _definition.ThreeState;

    public BoolFieldEditorViewModel(BoolFieldDefinition definition, BoolFieldValue value)
    {
        _definition = definition;
        _value = value;
        IsChecked = definition.ThreeState ? value.Value : value.Value ?? false;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) => IsChecked = data.Bool();

    public override FieldValue GetCurrentValue()
    {
        _value.Value = _definition.ThreeState ? IsChecked : IsChecked ?? false;
        return _value;
    }
}
