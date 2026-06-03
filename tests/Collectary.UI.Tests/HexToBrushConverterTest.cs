using System.Globalization;
using Avalonia.Media;
using Collectary.UI.Converters;

namespace Collectary.UI.Tests;

[TestFixture]
public class HexToBrushConverterTest
{
    [Test]
    public void Convert_ValidProducesBrush_InvalidProducesTransparent()
    {
        var brush = (SolidColorBrush)HexToBrushConverter.Instance.Convert(
            "#FF0000", typeof(IBrush), null, CultureInfo.InvariantCulture)!;
        Assert.That(brush.Color, Is.EqualTo(Colors.Red));

        var fallback = (SolidColorBrush)HexToBrushConverter.Instance.Convert(
            "nope", typeof(IBrush), null, CultureInfo.InvariantCulture)!;
        Assert.That(fallback.Color, Is.EqualTo(Colors.Transparent));

        var empty = (SolidColorBrush)HexToBrushConverter.Instance.Convert(
            "", typeof(IBrush), null, CultureInfo.InvariantCulture)!;
        Assert.That(empty.Color, Is.EqualTo(Colors.Transparent));
    }

    [Test]
    public void Convert_NullValue_ReturnsTransparent()
    {
        var brush = (SolidColorBrush)HexToBrushConverter.Instance.Convert(
            null, typeof(IBrush), null, CultureInfo.InvariantCulture)!;
        Assert.That(brush.Color, Is.EqualTo(Colors.Transparent));
    }

    [Test]
    public void Convert_NonStringValue_ReturnsTransparent()
    {
        var brush = (SolidColorBrush)HexToBrushConverter.Instance.Convert(
            42, typeof(IBrush), null, CultureInfo.InvariantCulture)!;
        Assert.That(brush.Color, Is.EqualTo(Colors.Transparent));
    }

    [Test]
    public void ConvertBack_Throws() =>
        Assert.Throws<NotSupportedException>(() =>
            HexToBrushConverter.Instance.ConvertBack(null, typeof(string), null, CultureInfo.InvariantCulture));
}
