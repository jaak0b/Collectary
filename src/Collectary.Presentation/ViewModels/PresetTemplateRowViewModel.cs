using CommunityToolkit.Mvvm.Input;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Templates;

namespace Collectary.Presentation.ViewModels;

public partial class PresetTemplateRowViewModel : ViewModelBase
{
    private readonly IPresetTemplate _template;
    private readonly Action<IPresetTemplate> _onChosen;

    public PresetTemplateRowViewModel(IPresetTemplate template, Action<IPresetTemplate> onChosen)
    {
        _template = template;
        _onChosen = onChosen;
    }

    public IPresetTemplate Template => _template;
    public string Name => LocalizationService.Instance[_template.NameKey];
    public string Description => LocalizationService.Instance[_template.DescriptionKey];
    public string Icon => _template.Icon;

    [RelayCommand]
    private void Select() => _onChosen(_template);
}
