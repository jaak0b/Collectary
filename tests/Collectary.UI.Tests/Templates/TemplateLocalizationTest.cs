using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Templates;

namespace Collectary.UI.Tests.Templates;

[TestFixture]
public class TemplateLocalizationTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    private static readonly string[] Languages = ["en", "de"];

    private static IEnumerable<string> ResolvedStrings(IPresetTemplate template)
    {
        yield return LocalizationService.Instance[template.NameKey];
        yield return LocalizationService.Instance[template.DescriptionKey];

        var preset = template.Build();
        yield return preset.Name;
        foreach (var field in preset.Fields)
        {
            yield return field.Label;
            foreach (var choice in ChoicesOf(field))
                yield return choice;
            if (field is ListFieldDefinition list)
                foreach (var sub in list.SubFields)
                    yield return sub.Label;
        }
    }

    private static IEnumerable<string> ChoicesOf(FieldDefinition field) => field switch
    {
        SingleChoiceFieldDefinition s => s.Choices.Select(c => c.Value),
        MultiChoiceFieldDefinition m => m.Choices.Select(c => c.Value),
        _ => Enumerable.Empty<string>()
    };

    [Test]
    public void EveryTemplateString_ResolvesInBothLanguages()
    {
        foreach (var language in Languages)
        {
            LocalizationService.Instance.Apply(language);
            foreach (var template in TemplateTestHelper.AllTemplates())
            {
                foreach (var value in ResolvedStrings(template))
                {
                    Assert.That(value, Is.Not.Null.And.Not.Empty,
                        $"[{language}] template '{template.Key}' has an empty resolved string");
                    Assert.That(value, Does.Not.StartWith("Tmpl_"),
                        $"[{language}] template '{template.Key}' is missing a resx key (resolved to the key itself: '{value}')");
                }
            }
        }
    }

    [Test]
    public void CategoryAndChromeKeys_ResolveInBothLanguages()
    {
        string[] keys =
        [
            "TemplateCategory_MediaEntertainment", "TemplateCategory_Collectibles",
            "TemplateCategory_Lifestyle", "TemplateCategory_Practical",
            "NewFromTemplate", "TemplatePickerTitle", "TemplatePickerSubtitle",
        ];
        foreach (var language in Languages)
        {
            LocalizationService.Instance.Apply(language);
            foreach (var key in keys)
            {
                var value = LocalizationService.Instance[key];
                Assert.That(value, Is.Not.Empty.And.Not.EqualTo(key),
                    $"[{language}] chrome/category key '{key}' is not defined");
            }
        }
    }
}
