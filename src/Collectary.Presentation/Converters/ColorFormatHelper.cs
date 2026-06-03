using System.Globalization;
using Avalonia.Media;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Services;

namespace Collectary.UI.Converters;

public static class ColorFormatHelper
{
    public static Color? ToColor(string? raw, ColorFormat format)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            return format switch
            {
                ColorFormat.Hex => Color.Parse(raw),
                ColorFormat.Rgb => ParseRgb(raw),
                ColorFormat.Argb => ParseArgb(raw),
                ColorFormat.Cmyk => CmykRawToColor(raw),
                _ => null
            };
        }
        catch (Exception ex)
        {
            AppLogger.Log.Warning(ex, "Failed to parse color value {Raw} as {Format}", raw, format);
            return null;
        }
    }

    public static string Encode(Color color, ColorFormat format) => format switch
    {
        ColorFormat.Hex => $"#{color.R:X2}{color.G:X2}{color.B:X2}",
        ColorFormat.Rgb => $"{color.R},{color.G},{color.B}",
        ColorFormat.Argb => $"{color.A},{color.R},{color.G},{color.B}",
        _ => $"#{color.R:X2}{color.G:X2}{color.B:X2}"
    };

    public static string EncodeCmyk(int c, int m, int y, int k) =>
        $"{Clamp(c, 100)},{Clamp(m, 100)},{Clamp(y, 100)},{Clamp(k, 100)}";

    public static (int c, int m, int y, int k) DecodeCmyk(string? raw)
    {
        var parts = Split(raw, 4);
        return (Clamp(parts[0], 100), Clamp(parts[1], 100), Clamp(parts[2], 100), Clamp(parts[3], 100));
    }

    public static Color CmykToColor(int c, int m, int y, int k)
    {
        var cf = Clamp(c, 100) / 100.0;
        var mf = Clamp(m, 100) / 100.0;
        var yf = Clamp(y, 100) / 100.0;
        var kf = Clamp(k, 100) / 100.0;
        var r = (byte)Math.Round(255 * (1 - cf) * (1 - kf));
        var g = (byte)Math.Round(255 * (1 - mf) * (1 - kf));
        var b = (byte)Math.Round(255 * (1 - yf) * (1 - kf));
        return Color.FromRgb(r, g, b);
    }

    private static Color ParseRgb(string raw)
    {
        var p = Split(raw, 3);
        return Color.FromRgb((byte)Clamp(p[0], 255), (byte)Clamp(p[1], 255), (byte)Clamp(p[2], 255));
    }

    private static Color ParseArgb(string raw)
    {
        var p = Split(raw, 4);
        return Color.FromArgb((byte)Clamp(p[0], 255), (byte)Clamp(p[1], 255), (byte)Clamp(p[2], 255), (byte)Clamp(p[3], 255));
    }

    private static Color CmykRawToColor(string raw)
    {
        var (c, m, y, k) = DecodeCmyk(raw);
        return CmykToColor(c, m, y, k);
    }

    private static int[] Split(string? raw, int count)
    {
        var result = new int[count];
        if (string.IsNullOrWhiteSpace(raw)) return result;
        var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < count && i < parts.Length; i++)
            int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]);
        return result;
    }

    private static int Clamp(int value, int max) => Math.Clamp(value, 0, max);
}
