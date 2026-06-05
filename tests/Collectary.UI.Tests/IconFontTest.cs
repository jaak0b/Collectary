using System.Reflection;
using Avalonia.Platform;
using Collectary.Core.Domain.Fields;
using SkiaSharp;

namespace Collectary.UI.Tests;

[TestFixture]
public class IconFontTest
{
    private static readonly Uri FontUri =
        new("avares://Collectary.UI/Assets/Fonts/CollectaryIcons.ttf");

    private static IEnumerable<(string Name, int Codepoint)> Glyphs() =>
        typeof(IconGlyphs).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true } && f.FieldType == typeof(string))
            .Select(f =>
            {
                var value = (string)f.GetRawConstantValue()!;
                return (f.Name, char.ConvertToUtf32(value, 0));
            });

    private static SKTypeface LoadFont()
    {
        using var stream = AssetLoader.Open(FontUri);
        return SKTypeface.FromStream(stream)
               ?? throw new InvalidOperationException("CollectaryIcons.ttf failed to load as a typeface.");
    }

    [Test]
    public void Font_IsEmbeddedAndNamedCollectaryIcons()
    {
        using var typeface = LoadFont();
        Assert.That(typeface.FamilyName, Is.EqualTo("CollectaryIcons"));
    }

    [Test]
    public void EveryIconGlyph_HasAGlyphInTheEmbeddedFont()
    {
        using var typeface = LoadFont();
        var missing = Glyphs()
            .Where(g => typeface.GetGlyph(g.Codepoint) == 0)
            .Select(g => $"{g.Name}=U+{g.Codepoint:X4}")
            .ToList();

        Assert.That(missing, Is.Empty,
            $"The embedded icon font is missing glyphs for: {string.Join(", ", missing)}");
    }
}
