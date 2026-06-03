using System.Globalization;
using Avalonia.Media;
using Collectary.UI.Converters;

namespace Collectary.UI.Tests;

[TestFixture]
public class HexToColorConverterTest
{
    [Test]
    public void Convert_ValidAndInvalid()
    {
        Assert.That(HexToColorConverter.Instance.Convert("#00FF00", typeof(Color), null, CultureInfo.InvariantCulture),
            Is.EqualTo(Colors.Lime));
        Assert.That(HexToColorConverter.Instance.Convert("bad", typeof(Color), null, CultureInfo.InvariantCulture),
            Is.EqualTo(Colors.Transparent));
    }

    [Test]
    public void Convert_NullValue_ReturnsTransparent() =>
        Assert.That(HexToColorConverter.Instance.Convert(null, typeof(Color), null, CultureInfo.InvariantCulture),
            Is.EqualTo(Colors.Transparent));

    [Test]
    public void Convert_EmptyString_ReturnsTransparent() =>
        Assert.That(HexToColorConverter.Instance.Convert("", typeof(Color), null, CultureInfo.InvariantCulture),
            Is.EqualTo(Colors.Transparent));

    [Test]
    public void ConvertBack_Throws() =>
        Assert.Throws<NotSupportedException>(() =>
            HexToColorConverter.Instance.ConvertBack(null, typeof(string), null, CultureInfo.InvariantCulture));
}
