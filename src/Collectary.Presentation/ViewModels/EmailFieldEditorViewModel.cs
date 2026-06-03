using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public partial class EmailFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly EmailFieldDefinition _definition;
    private readonly EmailFieldValue _value;

    [ObservableProperty]
    public partial string? Text { get; set; }

    public EmailFieldEditorViewModel(EmailFieldDefinition definition, EmailFieldValue value)
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
