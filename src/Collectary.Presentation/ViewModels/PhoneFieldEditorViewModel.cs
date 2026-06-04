using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class PhoneFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly PhoneFieldDefinition _definition;
    private readonly PhoneFieldValue _value;

    [ObservableProperty]
    public partial string? Text { get; set; }

    public PhoneFieldEditorViewModel(PhoneFieldDefinition definition, PhoneFieldValue value)
    {
        _definition = definition;
        _value = value;
        Text = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Value = Text;
        return _value;
    }
}
