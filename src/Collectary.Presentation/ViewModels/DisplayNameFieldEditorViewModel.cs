using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

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

    public override void Randomize(Services.ISampleData data) => Text = data.Words(2);

    public override FieldValue GetCurrentValue() => throw new NotSupportedException();
}
