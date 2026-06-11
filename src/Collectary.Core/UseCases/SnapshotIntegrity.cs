using System.Security.Cryptography;
using System.Text;

namespace Collectary.Core.UseCases;

public sealed class SnapshotIntegrity
{
    private const string Prefix = "sha256:";

    public string Wrap(string json) => Prefix + Hash(json) + "\n" + json;

    public string? HeaderHash(string content)
    {
        if (!content.StartsWith(Prefix, StringComparison.Ordinal)) return null;
        var newline = content.IndexOf('\n');
        return newline < 0 ? null : content[Prefix.Length..newline];
    }

    public bool TryUnwrap(string content, out string json)
    {
        json = content;
        if (!content.StartsWith(Prefix, StringComparison.Ordinal)) return false;

        var newline = content.IndexOf('\n');
        if (newline < 0) return false;

        var expected = content[Prefix.Length..newline];
        json = content[(newline + 1)..];
        return string.Equals(expected, Hash(json), StringComparison.OrdinalIgnoreCase);
    }

    private string Hash(string json) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
}
