using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class TextFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly TextFieldDefinition _definition;
    private readonly TextFieldValue _value;

    [ObservableProperty]
    public partial string? Text { get; set; }

    public int MaxLength => _definition.MaxLength ?? 0;

    public TextFieldEditorViewModel(TextFieldDefinition definition, TextFieldValue value)
    {
        _definition = definition;
        _value = value;
        Text = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) => Text = data.Words(2);

    public override FieldValue GetCurrentValue()
    {
        _value.Value = Text;
        return _value;
    }
}
