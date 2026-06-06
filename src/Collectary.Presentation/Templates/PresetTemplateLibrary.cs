namespace Collectary.Presentation.Templates;

public sealed class PresetTemplateLibrary : IPresetTemplateLibrary
{
    public PresetTemplateLibrary(IEnumerable<IPresetTemplate> templates)
    {
        All = templates.ToList();
    }

    public IReadOnlyList<IPresetTemplate> All { get; }

    public IReadOnlyList<PresetTemplateGroup> ByCategory()
    {
        var categoryOrder = new[]
        {
            PresetTemplateCategory.MediaEntertainment,
            PresetTemplateCategory.Collectibles,
            PresetTemplateCategory.Lifestyle,
            PresetTemplateCategory.Practical,
            PresetTemplateCategory.Developer,
        };
        var groups = new List<PresetTemplateGroup>();
        foreach (var category in categoryOrder)
        {
            var members = All.Where(t => t.Category == category).ToList();
            if (members.Count > 0)
                groups.Add(new PresetTemplateGroup(category, members));
        }
        return groups;
    }
}
