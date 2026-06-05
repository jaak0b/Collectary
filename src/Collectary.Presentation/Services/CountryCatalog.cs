using System.Globalization;

namespace Collectary.Presentation.Services;

/// <summary>A selectable country: its ISO 3166-1 alpha-2 <see cref="Code"/>, English <see cref="Name"/>, and <see cref="Flag"/> emoji.</summary>
public sealed record CountryOption(string Code, string Name, string Flag)
{
    public string Display => $"{Flag} {Name}";
}

/// <summary>
/// Builds the list of countries offered by the country field. Names come from <see cref="RegionInfo"/>
/// (always English, so deterministic), and the flag is derived from the code's regional-indicator letters.
/// </summary>
public class CountryCatalog : ICountryCatalog
{
    // A broad, hobby-relevant set (coins, stamps, wine, whisky origins). Codes are ISO 3166-1 alpha-2.
    private readonly string[] _codes =
    [
        "AR", "AT", "AU", "BE", "BR", "CA", "CH", "CL", "CN", "CO", "CZ", "DE", "DK", "EG", "ES",
        "FI", "FR", "GB", "GR", "HU", "IE", "IL", "IN", "IS", "IT", "JP", "KR", "MX", "NL", "NO",
        "NZ", "PL", "PT", "RO", "RU", "SE", "SG", "SK", "TH", "TR", "UA", "US", "ZA"
    ];

    private readonly IReadOnlyList<CountryOption> _countries;

    public CountryCatalog()
    {
        _countries = _codes
            .Select(code => new CountryOption(code, SafeName(code), ToFlag(code)))
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<CountryOption> Countries => _countries;

    public CountryOption? Find(string? code) =>
        code is null ? null : _countries.FirstOrDefault(c => c.Code == code);

    private string SafeName(string code)
    {
        try { return new RegionInfo(code).EnglishName; }
        catch (ArgumentException) { return code; }
    }

    /// <summary>Maps a two-letter country code to its flag by offsetting each letter into the regional-indicator block.</summary>
    public string ToFlag(string code)
    {
        if (code.Length != 2) return string.Empty;
        var upper = code.ToUpperInvariant();
        if (upper[0] < 'A' || upper[0] > 'Z' || upper[1] < 'A' || upper[1] > 'Z') return string.Empty;
        return char.ConvertFromUtf32(0x1F1E6 + (upper[0] - 'A'))
             + char.ConvertFromUtf32(0x1F1E6 + (upper[1] - 'A'));
    }
}

public interface ICountryCatalog
{
    IReadOnlyList<CountryOption> Countries { get; }
    CountryOption? Find(string? code);
}
