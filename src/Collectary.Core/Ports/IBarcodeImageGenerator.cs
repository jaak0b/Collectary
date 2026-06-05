namespace Collectary.Core.Ports;

/// <summary>Renders a text payload as a QR-code PNG image (used by the QR-code label field).</summary>
public interface IBarcodeImageGenerator
{
    /// <summary>Encodes <paramref name="content"/> as a square QR code of <paramref name="size"/> pixels, returned as PNG bytes.</summary>
    byte[] GenerateQrPng(string content, int size);
}
