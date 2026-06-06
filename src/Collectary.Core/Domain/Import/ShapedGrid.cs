namespace Collectary.Core.Domain.Import;

public sealed record ShapedGrid(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<WorkbookCell>> Rows);
