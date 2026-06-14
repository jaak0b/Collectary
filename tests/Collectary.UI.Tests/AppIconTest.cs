using Avalonia.Platform;
using SkiaSharp;

namespace Collectary.UI.Tests;

[TestFixture]
public class AppIconTest
{
    private static SKBitmap Decode(string assetUri)
    {
        using var stream = AssetLoader.Open(new Uri(assetUri));
        return SKBitmap.Decode(stream)
               ?? throw new InvalidOperationException($"{assetUri} failed to decode as an image.");
    }

    [Test]
    public void WindowIcon_IsA256SquarePng()
    {
        using var bitmap = Decode("avares://Collectary.UI/Assets/Icon.png");

        Assert.Multiple(() =>
        {
            Assert.That(bitmap.Width, Is.EqualTo(256));
            Assert.That(bitmap.Height, Is.EqualTo(256));
        });
    }

    [Test]
    public void ExecutableIcon_DecodesAsAValidImage()
    {
        using var bitmap = Decode("avares://Collectary.UI/Assets/collectary.ico");

        Assert.That(bitmap.Width, Is.GreaterThan(0));
    }
}
