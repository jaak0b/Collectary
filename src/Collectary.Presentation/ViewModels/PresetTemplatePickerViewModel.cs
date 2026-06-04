using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Templates;

namespace Collectary.Presentation.ViewModels;

public partial class PresetTemplatePickerViewModel : ViewModelBase
{
    private readonly Action<Preset> _onTemplateChosen;
    private readonly Action _onCancel;

    public ObservableCollection<PresetTemplateCategoryViewModel> Categories { get; } = new();

    public PresetTemplatePickerViewModel(
        IPresetTemplateLibrary library,
        Action<Preset> onTemplateChosen,
        Action onCancel)
    {
        _onTemplateChosen = onTemplateChosen;
        _onCancel = onCancel;

        foreach (var group in library.ByCategory())
        {
            var header = LocalizationService.Instance[CategoryKey(group.Category)];
            var rows = group.Templates
                .Select(t => new PresetTemplateRowViewModel(t, Choose))
                .ToList();
            Categories.Add(new PresetTemplateCategoryViewModel(header, rows));
        }
    }

    private void Choose(IPresetTemplate template) => _onTemplateChosen(template.Build());

    [RelayCommand]
    private void Cancel() => _onCancel();

    private string CategoryKey(PresetTemplateCategory category) => category switch
    {
        PresetTemplateCategory.MediaEntertainment => "TemplateCategory_MediaEntertainment",
        PresetTemplateCategory.Collectibles => "TemplateCategory_Collectibles",
        PresetTemplateCategory.Lifestyle => "TemplateCategory_Lifestyle",
        PresetTemplateCategory.Practical => "TemplateCategory_Practical",
        _ => category.ToString()
    };
}
