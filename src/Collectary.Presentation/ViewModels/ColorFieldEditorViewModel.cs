using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public class ColorFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly ColorFieldDefinition _definition;
    private readonly ColorFieldValue _fieldValue;

    public ColorFormatEditorViewModel SubEditor { get; }

    public ColorFieldEditorViewModel(
        ColorFieldDefinition definition,
        ColorFieldValue value,
        ColorFormatEditorFactory colorFormatFactory)
    {
        _definition = definition;
        _fieldValue = value;
        SubEditor = colorFormatFactory.Create(definition.Format, value.Value);
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Value = SubEditor.Encode();
        return _fieldValue;
    }
}
