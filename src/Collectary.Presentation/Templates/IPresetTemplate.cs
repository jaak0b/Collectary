using Collectary.Core.Domain;

namespace Collectary.Presentation.Templates;

public interface IPresetTemplate
{
    string Key { get; }
    PresetTemplateCategory Category { get; }
    string Icon { get; }
    string NameKey { get; }
    string DescriptionKey { get; }
    Preset Build();
}
