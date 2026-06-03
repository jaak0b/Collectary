using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Localization;

namespace Collectary.UI.ViewModels;

public partial class DisplayNameFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly DisplayNameFieldDefinition _definition;

    [ObservableProperty]
    public partial string? Text { get; set; }

    public DisplayNameFieldEditorViewModel(DisplayNameFieldDefinition definition, string currentName)
    {
        _definition = definition;
        Text = currentName;
    }

    public string Label => LocalizationService.Instance["DisplayNameField"];

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue() => throw new NotSupportedException();
}
