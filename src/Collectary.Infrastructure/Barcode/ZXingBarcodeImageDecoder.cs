using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Collectary.Infrastructure.Barcode;

/// <summary>
/// Decodes a barcode/QR from an encoded still image (PNG/JPEG bytes) using ZXing.Net,
/// with SkiaSharp turning the bytes into a pixel buffer. Used by the snapshot scan flow.
/// </summary>
public class ZXingBarcodeImageDecoder : IBarcodeImageDecoder
{
    private readonly BarcodeReaderGeneric _reader = new()
    {
        AutoRotate = true,
        Options = new DecodingOptions { TryHarder = true }
    };

    public BarcodeReadResult? Decode(byte[] imageBytes)
    {
        if (imageBytes is null || imageBytes.Length == 0) return null;

        using var data = SKData.CreateCopy(imageBytes);
        using var codec = SKCodec.Create(data);
        if (codec is null) return null;

        // Decode straight into a known BGRA layout so the luminance source always gets the format it expects.
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = SKBitmap.Decode(codec, info);
        if (bitmap is null) return null;

        var luminance = new RGBLuminanceSource(
            bitmap.GetPixelSpan().ToArray(), bitmap.Width, bitmap.Height,
            RGBLuminanceSource.BitmapFormat.BGRA32);

        var result = _reader.Decode(luminance);
        if (result is null) return null;

        return new BarcodeReadResult(result.Text, Map(result.BarcodeFormat));
    }

    private BarcodeSymbology Map(BarcodeFormat format) => format switch
    {
        BarcodeFormat.EAN_13 => BarcodeSymbology.Ean13,
        BarcodeFormat.EAN_8 => BarcodeSymbology.Ean8,
        BarcodeFormat.UPC_A => BarcodeSymbology.UpcA,
        BarcodeFormat.UPC_E => BarcodeSymbology.UpcE,
        BarcodeFormat.CODE_39 => BarcodeSymbology.Code39,
        BarcodeFormat.CODE_93 => BarcodeSymbology.Code93,
        BarcodeFormat.CODE_128 => BarcodeSymbology.Code128,
        BarcodeFormat.ITF => BarcodeSymbology.Itf,
        BarcodeFormat.CODABAR => BarcodeSymbology.Codabar,
        BarcodeFormat.QR_CODE => BarcodeSymbology.QrCode,
        BarcodeFormat.DATA_MATRIX => BarcodeSymbology.DataMatrix,
        BarcodeFormat.AZTEC => BarcodeSymbology.Aztec,
        BarcodeFormat.PDF_417 => BarcodeSymbology.Pdf417,
        _ => BarcodeSymbology.Unknown
    };
}
