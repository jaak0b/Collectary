using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public partial class CurrencyFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly CurrencyFieldDefinition _definition;
    private readonly CurrencyFieldValue _value;

    [ObservableProperty]
    public partial decimal? Amount { get; set; }

    public string CurrencySymbol => _definition.CurrencySymbol;

    public CurrencyFieldEditorViewModel(CurrencyFieldDefinition definition, CurrencyFieldValue value)
    {
        _definition = definition;
        _value = value;
        Amount = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Value = Amount;
        return _value;
    }
}
