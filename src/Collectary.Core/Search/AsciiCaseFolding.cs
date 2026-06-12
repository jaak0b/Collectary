namespace Collectary.Core.Search;

public class AsciiCaseFolding
{
    public string Fold(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= 'A' and <= 'Z')
                chars[i] = (char)(chars[i] + 32);
        }
        return new string(chars);
    }

    public bool AreEqual(string? left, string right) =>
        left is not null && Fold(left).Equals(Fold(right), StringComparison.Ordinal);

    public bool Contains(string? haystack, string needle) =>
        haystack is not null && Fold(haystack).Contains(Fold(needle), StringComparison.Ordinal);
}
