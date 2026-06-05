using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Ports;

/// <summary>The outcome of decoding a single still image: the textual payload and the symbology it was read as.</summary>
public sealed record BarcodeReadResult(string Code, BarcodeSymbology Symbology);

/// <summary>
/// Decodes a barcode/QR payload from a single still image (the "snapshot" scan model).
/// Acquiring the image is the platform's job; turning its bytes into a code is this port's.
/// </summary>
public interface IBarcodeImageDecoder
{
    /// <summary>Returns the first barcode found in <paramref name="imageBytes"/>, or null if none decodes.</summary>
    BarcodeReadResult? Decode(byte[] imageBytes);
}
