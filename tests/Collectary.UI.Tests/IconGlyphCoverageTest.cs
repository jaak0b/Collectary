using System.Reflection;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Templates;

namespace Collectary.UI.Tests;

[TestFixture]
public class IconGlyphCoverageTest
{
    private static HashSet<string> GlyphSet() =>
        typeof(IconGlyphs).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

    private static IEnumerable<Type> ConcreteFieldDefinitions() =>
        typeof(FieldDefinition).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(FieldDefinition).IsAssignableFrom(t));

    [Test]
    public void EveryFieldType_UsesAnIconGlyphConstant()
    {
        var glyphs = GlyphSet();
        var offenders = ConcreteFieldDefinitions()
            .Where(t => !glyphs.Contains(t.GetFieldIcon()))
            .Select(t => t.Name)
            .ToList();

        Assert.That(offenders, Is.Empty,
            $"These field types must use an IconGlyphs constant for [FieldIcon]: {string.Join(", ", offenders)}");
    }

    [Test]
    public void EveryTemplate_UsesAnIconGlyphConstant()
    {
        var glyphs = GlyphSet();
        var offenders = TemplateTestHelper.AllTemplates()
            .Where(t => !glyphs.Contains(t.Icon))
            .Select(t => t.GetType().Name)
            .ToList();

        Assert.That(offenders, Is.Empty,
            $"These templates must use an IconGlyphs constant for Icon: {string.Join(", ", offenders)}");
    }

    [Test]
    public void GroupRowIcon_UsesAnIconGlyphConstant() =>
        Assert.That(GlyphSet(), Does.Contain(new FieldGroupRowViewModel("G").TypeIcon));
}
