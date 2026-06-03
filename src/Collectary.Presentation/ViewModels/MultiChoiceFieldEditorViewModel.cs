using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public partial class MultiChoiceItemViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public string Label { get; }

    public MultiChoiceItemViewModel(string label, bool isSelected)
    {
        Label = label;
        IsSelected = isSelected;
    }
}

public partial class MultiChoiceFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly MultiChoiceFieldDefinition _definition;
    private readonly MultiChoiceFieldValue _fieldValue;

    public ObservableCollection<MultiChoiceItemViewModel> ChoiceItems { get; }

    public MultiChoiceFieldEditorViewModel(MultiChoiceFieldDefinition definition, MultiChoiceFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        ChoiceItems = new ObservableCollection<MultiChoiceItemViewModel>(
            definition.Choices.OrderBy(c => c.DisplayOrder)
                .Select(c => new MultiChoiceItemViewModel(c.Value, value.Selected.Contains(c.Value))));
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Selected = ChoiceItems.Where(c => c.IsSelected).Select(c => c.Label).ToList();
        return _fieldValue;
    }
}
