using System.Globalization;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases.Import;

public sealed class CultureDetector : ICultureDetector
{
    public CultureInfo Detect(IReadOnlyList<IReadOnlyList<WorkbookCell>> rows, IReadOnlyList<CultureInfo> candidates, CultureInfo fallback)
    {
        var samples = rows
            .SelectMany(row => row)
            .Where(cell => cell.Kind == WorkbookCellKind.Text && !string.IsNullOrWhiteSpace(cell.Text) && cell.Text!.Any(char.IsDigit))
            .Select(cell => cell.Text!)
            .Take(200)
            .ToList();

        if (samples.Count == 0) return fallback;

        var best = fallback;
        var bestScore = -1;
        foreach (var culture in candidates)
        {
            var score = samples.Count(sample => ParsesAsNumberOrDate(sample, culture));
            if (score > bestScore)
            {
                bestScore = score;
                best = culture;
            }
        }

        var fallbackScore = samples.Count(sample => ParsesAsNumberOrDate(sample, fallback));
        return fallbackScore >= bestScore ? fallback : best;
    }

    private bool ParsesAsNumberOrDate(string sample, CultureInfo culture) =>
        decimal.TryParse(sample, NumberStyles.Number, culture, out _)
        || DateTime.TryParse(sample, culture, DateTimeStyles.None, out _);
}
