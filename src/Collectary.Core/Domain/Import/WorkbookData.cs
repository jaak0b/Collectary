namespace Collectary.Core.Domain.Import;

public enum WorkbookCellKind
{
    Blank,
    Text,
    Number,
    DateTime,
    Boolean
}

public sealed record WorkbookCell(string? Text, WorkbookCellKind Kind);

public sealed record WorkbookSheet(string Name, IReadOnlyList<IReadOnlyList<WorkbookCell>> Rows);

public sealed record WorkbookData(IReadOnlyList<WorkbookSheet> Sheets);
