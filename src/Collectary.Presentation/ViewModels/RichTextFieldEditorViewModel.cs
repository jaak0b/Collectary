using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class RichTextFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly RichTextFieldDefinition _definition;
    private readonly RichTextFieldValue _value;

    [ObservableProperty]
    public partial string? Markdown { get; set; }

    public RichTextFieldEditorViewModel(RichTextFieldDefinition definition, RichTextFieldValue value)
    {
        _definition = definition;
        _value = value;
        Markdown = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) => Markdown = data.Sentence();

    public override FieldValue GetCurrentValue()
    {
        _value.Value = Markdown;
        return _value;
    }
}
