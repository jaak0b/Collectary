using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public partial class IntegerFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly IntegerFieldDefinition _definition;
    private readonly IntegerFieldValue _fieldValue;

    [ObservableProperty]
    public partial int? Number { get; set; }

    public IntegerFieldEditorViewModel(IntegerFieldDefinition definition, IntegerFieldValue value)
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
