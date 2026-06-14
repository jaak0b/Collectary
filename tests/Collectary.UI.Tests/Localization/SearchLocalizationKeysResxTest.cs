using Collectary.Presentation.Localization;
using Collectary.Search.Avalonia;

namespace Collectary.UI.Tests.Localization;

[TestFixture]
public class SearchLocalizationKeysResxTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    private static string Resolve(string language, string key)
    {
        LocalizationService.Instance.Apply(language);
        return LocalizationService.Instance[key];
    }

    [Test]
    public void EverySearchPackageKey_ResolvesInBothLanguages()
    {
        foreach (var key in new SearchLocalizationKeys().All)
        {
            var english = Resolve("en", key);
            var german = Resolve("de", key);
            Assert.Multiple(() =>
            {
                Assert.That(english, Is.Not.Empty, $"[en] search package key '{key}' resolves to an empty string");
                Assert.That(german, Is.Not.Empty, $"[de] search package key '{key}' resolves to an empty string");
                Assert.That(english == key && german == key, Is.False,
                    $"search package key '{key}' is not defined in the resx (both languages echo the key)");
            });
        }
    }
}
