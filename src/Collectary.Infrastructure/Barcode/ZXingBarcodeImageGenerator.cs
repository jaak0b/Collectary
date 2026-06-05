using Collectary.Core.Ports;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Collectary.Infrastructure.Barcode;

/// <summary>Generates QR-code PNGs with ZXing.Net, rendering the bit matrix through SkiaSharp.</summary>
public class ZXingBarcodeImageGenerator : IBarcodeImageGenerator
{
    public byte[] GenerateQrPng(string content, int size)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Width = size, Height = size, Margin = 1 }
        };
        var pixelData = writer.Write(content ?? string.Empty);

        using var bitmap = new SKBitmap(new SKImageInfo(
            pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        System.Runtime.InteropServices.Marshal.Copy(
            pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
