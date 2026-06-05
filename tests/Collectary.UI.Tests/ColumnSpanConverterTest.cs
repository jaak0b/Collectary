using System.Globalization;
using Collectary.Presentation.Converters;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests;

[TestFixture]
public class ColumnSpanConverterTest
{
    [Test]
    public void Convert_SpanOfOne_UsesSingularResource() =>
        Assert.That(
            ColumnSpanConverter.Instance.Convert(1, typeof(string), null, CultureInfo.InvariantCulture),
            Is.EqualTo(LocalizationService.Instance["ColumnSpan_1"]));

    [Test]
    public void Convert_SpanGreaterThanOne_FormatsPluralResource() =>
        Assert.That(
            ColumnSpanConverter.Instance.Convert(3, typeof(string), null, CultureInfo.InvariantCulture),
            Is.EqualTo(string.Format(LocalizationService.Instance["ColumnSpan_N"], 3)));

    [Test]
    public void Convert_NonInteger_PassesThrough() =>
        Assert.That(
            ColumnSpanConverter.Instance.Convert("x", typeof(string), null, CultureInfo.InvariantCulture),
            Is.EqualTo("x"));

    [Test]
    public void ConvertBack_Throws() =>
        Assert.Throws<NotSupportedException>(() =>
            ColumnSpanConverter.Instance.ConvertBack(null, typeof(int), null, CultureInfo.InvariantCulture));
}
