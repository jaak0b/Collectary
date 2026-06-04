namespace Collectary.Presentation.Templates;

public interface IPresetTemplateLibrary
{
    IReadOnlyList<IPresetTemplate> All { get; }

    IReadOnlyList<PresetTemplateGroup> ByCategory();
}

public sealed class PresetTemplateGroup
{
    public PresetTemplateGroup(PresetTemplateCategory category, IReadOnlyList<IPresetTemplate> templates)
    {
        Category = category;
        Templates = templates;
    }

    public PresetTemplateCategory Category { get; }
    public IReadOnlyList<IPresetTemplate> Templates { get; }
}
