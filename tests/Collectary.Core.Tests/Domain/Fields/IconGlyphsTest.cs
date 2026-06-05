using System.Reflection;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class IconGlyphsTest
{
    private static IEnumerable<FieldInfo> Constants() =>
        typeof(IconGlyphs).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string));

    [Test]
    public void HasConstants() =>
        Assert.That(Constants().Count(), Is.GreaterThan(0), "IconGlyphs must declare glyph constants.");

    [Test]
    public void EveryGlyph_IsASinglePrivateUseScalar()
    {
        var offenders = new List<string>();
        foreach (var field in Constants())
        {
            var value = (string)field.GetRawConstantValue()!;
            if (string.IsNullOrEmpty(value))
            {
                offenders.Add($"{field.Name}=<empty>");
                continue;
            }
            var isSingleScalar = value.Length == 1 || (value.Length == 2 && char.IsSurrogatePair(value, 0));
            var cp = char.ConvertToUtf32(value, 0);
            var inPrivateUse = cp is >= 0xE000 and <= 0xF8FF;
            if (!isSingleScalar || !inPrivateUse)
                offenders.Add($"{field.Name}=U+{cp:X4}");
        }

        Assert.That(offenders, Is.Empty,
            $"Glyphs must be a single Private-Use-Area scalar (U+E000..U+F8FF): {string.Join(", ", offenders)}");
    }

    [Test]
    public void Glyphs_AreUnique()
    {
        var duplicates = Constants()
            .Select(f => (string)f.GetRawConstantValue()!)
            .GroupBy(v => v)
            .Where(g => g.Count() > 1)
            .Select(g => $"U+{char.ConvertToUtf32(g.Key, 0):X4}")
            .ToList();

        Assert.That(duplicates, Is.Empty, $"Duplicate glyph codepoints: {string.Join(", ", duplicates)}");
    }
}
