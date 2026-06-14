using Collectary.Search.Avalonia;

namespace Collectary.Presentation.Localization;

public sealed class LocalizationProvider : ILocalizationProvider
{
    public string Get(string key) => LocalizationService.Instance[key];
}
