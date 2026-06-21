using System.Globalization;

namespace Collectary.Presentation.ViewModels;

public class DateRangeTextFormatter
{
    private const string MissingEnd = "…";

    public string Format(DateTime? from, DateTime? to, CultureInfo culture) =>
        $"{from?.ToString("d", culture) ?? MissingEnd} → {to?.ToString("d", culture) ?? MissingEnd}";
}
