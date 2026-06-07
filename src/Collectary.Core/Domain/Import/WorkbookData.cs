using System.Globalization;

namespace Collectary.Core.Domain.Import;

public enum WorkbookCellKind
{
    Blank,
    Text,
    Number,
    DateTime,
    Boolean
}

public sealed record WorkbookCell(string? Text, WorkbookCellKind Kind)
{
    public IFormatProvider EffectiveCulture(CultureInfo culture) =>
        Kind is WorkbookCellKind.Number or WorkbookCellKind.DateTime or WorkbookCellKind.Boolean
            ? CultureInfo.InvariantCulture
            : culture;
}

public sealed record WorkbookSheet(string Name, IReadOnlyList<IReadOnlyList<WorkbookCell>> Rows);

public sealed record WorkbookData(IReadOnlyList<WorkbookSheet> Sheets);
