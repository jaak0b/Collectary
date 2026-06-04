using Avalonia.Media;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Converters;

namespace Collectary.UI.Tests;

[TestFixture]
public class ColorFormatHelperTest
{
    [Test]
    public void ToColor_NullOrWhitespace_ReturnsNull()
    {
        Assert.That(ColorFormatHelper.ToColor(null, ColorFormat.Hex), Is.Null);
        Assert.That(ColorFormatHelper.ToColor("   ", ColorFormat.Rgb), Is.Null);
    }

    [Test]
    public void ToColor_Hex_Parses()
    {
        var c = ColorFormatHelper.ToColor("#FF8000", ColorFormat.Hex);
        Assert.That(c, Is.Not.Null);
        Assert.That((c!.Value.R, c.Value.G, c.Value.B), Is.EqualTo(((byte)0xFF, (byte)0x80, (byte)0x00)));
    }

    [Test]
    public void ToColor_Rgb_ParsesAndClamps()
    {
        var c = ColorFormatHelper.ToColor("300, 20, 40", ColorFormat.Rgb)!.Value;
        Assert.That((c.R, c.G, c.B), Is.EqualTo(((byte)255, (byte)20, (byte)40)));
    }

    [Test]
    public void ToColor_Argb_Parses()
    {
        var c = ColorFormatHelper.ToColor("128,10,20,30", ColorFormat.Argb)!.Value;
        Assert.That((c.A, c.R, c.G, c.B), Is.EqualTo(((byte)128, (byte)10, (byte)20, (byte)30)));
    }

    [Test]
    public void ToColor_Cmyk_Parses()
    {
        var c = ColorFormatHelper.ToColor("0,0,0,0", ColorFormat.Cmyk)!.Value;
        Assert.That((c.R, c.G, c.B), Is.EqualTo(((byte)255, (byte)255, (byte)255)));
    }

    [Test]
    public void ToColor_InvalidHex_ReturnsNull() =>
        Assert.That(ColorFormatHelper.ToColor("not-a-color", ColorFormat.Hex), Is.Null);

    [Test]
    public void Encode_PerFormat()
    {
        var color = Color.FromArgb(128, 255, 128, 0);
        Assert.That(ColorFormatHelper.Encode(color, ColorFormat.Hex), Is.EqualTo("#FF8000"));
        Assert.That(ColorFormatHelper.Encode(color, ColorFormat.Rgb), Is.EqualTo("255,128,0"));
        Assert.That(ColorFormatHelper.Encode(color, ColorFormat.Argb), Is.EqualTo("128,255,128,0"));
        Assert.That(ColorFormatHelper.Encode(color, ColorFormat.Cmyk), Is.EqualTo("#FF8000"));
    }

    [Test]
    public void EncodeCmyk_Clamps() =>
        Assert.That(ColorFormatHelper.EncodeCmyk(150, -5, 50, 200), Is.EqualTo("100,0,50,100"));

    [Test]
    public void DecodeCmyk_ParsesAndClamps_AndHandlesNull()
    {
        Assert.That(ColorFormatHelper.DecodeCmyk("10,20,30,40"), Is.EqualTo((10, 20, 30, 40)));
        Assert.That(ColorFormatHelper.DecodeCmyk("999,0,0,0"), Is.EqualTo((100, 0, 0, 0)));
        Assert.That(ColorFormatHelper.DecodeCmyk(null), Is.EqualTo((0, 0, 0, 0)));
    }

    [Test]
    public void CmykToColor_PureCyan()
    {
        var c = ColorFormatHelper.CmykToColor(100, 0, 0, 0);
        Assert.That((c.R, c.G, c.B), Is.EqualTo(((byte)0, (byte)255, (byte)255)));
    }

    [Test]
    public void CmykToColor_PureMagenta_OnlyAffectsGreen()
    {
        var c = ColorFormatHelper.CmykToColor(0, 100, 0, 0);
        Assert.That((c.R, c.G, c.B), Is.EqualTo(((byte)255, (byte)0, (byte)255)));
    }

    [Test]
    public void CmykToColor_PureYellow_OnlyAffectsBlue()
    {
        var c = ColorFormatHelper.CmykToColor(0, 0, 100, 0);
        Assert.That((c.R, c.G, c.B), Is.EqualTo(((byte)255, (byte)255, (byte)0)));
    }

    [Test]
    public void CmykToColor_FullBlack_AllChannelsZero()
    {
        var c = ColorFormatHelper.CmykToColor(0, 0, 0, 100);
        Assert.That((c.R, c.G, c.B), Is.EqualTo(((byte)0, (byte)0, (byte)0)));
    }

    [Test]
    public void CmykToColor_HalfKey_HalvesEachChannel()
    {
        var c = ColorFormatHelper.CmykToColor(0, 0, 0, 50);
        Assert.That((c.R, c.G, c.B), Is.EqualTo(((byte)128, (byte)128, (byte)128)));
    }

    [Test]
    public void DecodeCmyk_WithFewerParts_FillsRemainderWithZero()
    {
        Assert.That(ColorFormatHelper.DecodeCmyk("10,20"), Is.EqualTo((10, 20, 0, 0)));
    }

    [Test]
    public void DecodeCmyk_NonNumericPart_TreatedAsZero()
    {
        Assert.That(ColorFormatHelper.DecodeCmyk("x,30,40,50"), Is.EqualTo((0, 30, 40, 50)));
    }

    [Test]
    public void ToColor_RgbWithNonNumeric_TreatsAsZero()
    {
        var c = ColorFormatHelper.ToColor("x,20,40", ColorFormat.Rgb)!.Value;
        Assert.That((c.R, c.G, c.B), Is.EqualTo(((byte)0, (byte)20, (byte)40)));
    }

    [Test]
    public void Encode_DefaultFormat_FallsBackToHex()
    {
        var color = Color.FromArgb(255, 1, 2, 3);
        Assert.That(ColorFormatHelper.Encode(color, (ColorFormat)999), Is.EqualTo("#010203"));
    }
}
