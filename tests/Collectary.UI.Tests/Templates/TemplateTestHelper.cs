using Collectary.Presentation.Templates;

namespace Collectary.UI.Tests.Templates;

internal static class TemplateTestHelper
{
    public static IReadOnlyList<IPresetTemplate> AllTemplates() =>
        typeof(IPresetTemplate).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(IPresetTemplate).IsAssignableFrom(t))
            .Select(t => (IPresetTemplate)Activator.CreateInstance(t)!)
            .ToList();

    public static PresetTemplateLibrary Library() => new(AllTemplates());
}
