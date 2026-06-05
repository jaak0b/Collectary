using Collectary.Core.Domain.Fields;
using Collectary.Infrastructure.Barcode;

namespace Collectary.Infrastructure.Tests.Barcode;

[TestFixture]
public class ZXingBarcodeImageGeneratorTest
{
    private readonly ZXingBarcodeImageGenerator _sut = new();
    private readonly ZXingBarcodeImageDecoder _decoder = new();

    [Test]
    public void GenerateQrPng_ProducesAQrThatDecodesBackToContent()
    {
        var png = _sut.GenerateQrPng("https://collectary.app/i/7", 320);

        var decoded = _decoder.Decode(png);

        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded!.Code, Is.EqualTo("https://collectary.app/i/7"));
        Assert.That(decoded.Symbology, Is.EqualTo(BarcodeSymbology.QrCode));
    }

    [Test]
    public void GenerateQrPng_ReturnsNonEmptyPngBytes()
    {
        var png = _sut.GenerateQrPng("BOX-42", 200);
        Assert.That(png, Is.Not.Empty);
        // PNG signature
        Assert.That(png[0], Is.EqualTo(0x89));
        Assert.That(png[1], Is.EqualTo((byte)'P'));
    }
}
