using System.Globalization;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Converters;

namespace Collectary.UI.Tests;

[TestFixture]
public class ImageSizeModeConverterTest
{
    [Test]
    public void Convert_ConvertsKnownMode_PassesThroughOther()
    {
        var result = ImageSizeModeConverter.Instance.Convert(
            ImageSizeMode.Fixed, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(result, Is.Not.Null);

        var other = ImageSizeModeConverter.Instance.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(other, Is.EqualTo(42));
    }

    [Test]
    public void ConvertBack_Throws() =>
        Assert.Throws<NotSupportedException>(() =>
            ImageSizeModeConverter.Instance.ConvertBack(null, typeof(ImageSizeMode), null, CultureInfo.InvariantCulture));
}
