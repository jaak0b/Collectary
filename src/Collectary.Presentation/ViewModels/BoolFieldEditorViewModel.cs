using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public partial class BoolFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly BoolFieldDefinition _definition;
    private readonly BoolFieldValue _value;

    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    public BoolFieldEditorViewModel(BoolFieldDefinition definition, BoolFieldValue value)
    {
        _definition = definition;
        _value = value;
        IsChecked = value.Value ?? false;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Value = IsChecked;
        return _value;
    }
}
