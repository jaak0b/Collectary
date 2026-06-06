using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class SingleChoiceFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly SingleChoiceFieldDefinition _definition;
    private readonly SingleChoiceFieldValue _fieldValue;

    [ObservableProperty]
    public partial string? Selected { get; set; }

    public IReadOnlyList<string> Choices => _definition.Choices.OrderBy(c => c.DisplayOrder).Select(c => c.Value).ToList();

    public SingleChoiceFieldEditorViewModel(SingleChoiceFieldDefinition definition, SingleChoiceFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Selected = value.Selected;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data)
    {
        if (Choices.Count > 0)
            Selected = data.PickOne(Choices);
    }

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Selected = Selected;
        return _fieldValue;
    }
}
