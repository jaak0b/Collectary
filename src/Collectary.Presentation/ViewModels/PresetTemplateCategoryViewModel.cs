namespace Collectary.Presentation.ViewModels;

public class PresetTemplateCategoryViewModel : ViewModelBase
{
    public PresetTemplateCategoryViewModel(string header, IReadOnlyList<PresetTemplateRowViewModel> templates)
    {
        Header = header;
        Templates = templates;
    }

    public string Header { get; }
    public IReadOnlyList<PresetTemplateRowViewModel> Templates { get; }
}
