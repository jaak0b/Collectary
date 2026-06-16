using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests.Localization;

[TestFixture]
public class EditorButtonLocalizationTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    private static string Resolve(string language, string key)
    {
        LocalizationService.Instance.Apply(language);
        return LocalizationService.Instance[key];
    }

    [Test]
    public void SaveAndBackKey_ResolvesInBothLanguages()
    {
        var english = Resolve("en", "SaveAndBack");
        var german = Resolve("de", "SaveAndBack");

        Assert.Multiple(() =>
        {
            Assert.That(english, Is.Not.Empty.And.Not.EqualTo("SaveAndBack"),
                "[en] 'SaveAndBack' is not defined in Strings.en.resx");
            Assert.That(german, Is.Not.Empty.And.Not.EqualTo("SaveAndBack"),
                "[de] 'SaveAndBack' is not defined in Strings.de.resx");
        });
    }
}
