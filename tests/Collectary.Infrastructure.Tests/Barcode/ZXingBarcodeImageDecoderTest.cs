using Collectary.Core.Domain.Fields;
using Collectary.Infrastructure.Barcode;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Collectary.Infrastructure.Tests.Barcode;

[TestFixture]
public class ZXingBarcodeImageDecoderTest
{
    private readonly ZXingBarcodeImageDecoder _sut = new();

    private static byte[] EncodePng(BarcodeFormat format, string payload, int width, int height)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = format,
            Options = new EncodingOptions { Width = width, Height = height, Margin = 4 }
        };
        var pixelData = writer.Write(payload);
        using var bitmap = new SKBitmap(new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Test]
    public void Decode_QrCode_ReturnsPayloadAndSymbology()
    {
        var bytes = EncodePng(BarcodeFormat.QR_CODE, "https://collectary.app/item/42", 300, 300);

        var result = _sut.Decode(bytes);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Code, Is.EqualTo("https://collectary.app/item/42"));
        Assert.That(result.Symbology, Is.EqualTo(BarcodeSymbology.QrCode));
    }

    [Test]
    public void Decode_Ean13_ReturnsPayloadAndSymbology()
    {
        var bytes = EncodePng(BarcodeFormat.EAN_13, "5901234123457", 400, 200);

        var result = _sut.Decode(bytes);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Code, Is.EqualTo("5901234123457"));
        Assert.That(result.Symbology, Is.EqualTo(BarcodeSymbology.Ean13));
    }

    [Test]
    public void Decode_ImageWithNoBarcode_ReturnsNull()
    {
        using var blank = new SKBitmap(new SKImageInfo(120, 120, SKColorType.Bgra8888, SKAlphaType.Premul));
        blank.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(blank);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        Assert.That(_sut.Decode(data.ToArray()), Is.Null);
    }

    [Test]
    public void Decode_GarbageBytes_ReturnsNull() =>
        Assert.That(_sut.Decode(new byte[] { 1, 2, 3, 4, 5 }), Is.Null);

    [Test]
    public void Decode_NullOrEmpty_ReturnsNull()
    {
        Assert.That(_sut.Decode(null!), Is.Null);
        Assert.That(_sut.Decode(Array.Empty<byte>()), Is.Null);
    }

    [TestCase(BarcodeFormat.CODE_128, "ABC-12345", BarcodeSymbology.Code128)]
    [TestCase(BarcodeFormat.CODE_39, "CODE39", BarcodeSymbology.Code39)]
    [TestCase(BarcodeFormat.CODE_93, "CODE93", BarcodeSymbology.Code93)]
    [TestCase(BarcodeFormat.ITF, "1234567890", BarcodeSymbology.Itf)]
    [TestCase(BarcodeFormat.PDF_417, "PDF417-PAYLOAD", BarcodeSymbology.Pdf417)]
    [TestCase(BarcodeFormat.DATA_MATRIX, "DM-PAYLOAD", BarcodeSymbology.DataMatrix)]
    [TestCase(BarcodeFormat.AZTEC, "AZTEC-PAYLOAD", BarcodeSymbology.Aztec)]
    public void Decode_EachSymbology_MapsAndReadsBack(BarcodeFormat format, string payload, BarcodeSymbology expected)
    {
        var bytes = EncodePng(format, payload, 600, 300);

        var result = _sut.Decode(bytes);

        Assert.That(result, Is.Not.Null, $"{format} did not decode");
        Assert.That(result!.Code, Is.EqualTo(payload));
        Assert.That(result.Symbology, Is.EqualTo(expected));
    }
}
