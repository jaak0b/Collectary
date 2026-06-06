using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases.Import;

public sealed class GridShaper : IGridShaper
{
    public ShapedGrid Shape(IReadOnlyList<IReadOnlyList<WorkbookCell>> rows, bool transpose, bool firstRowIsHeader)
    {
        var oriented = Rectangularize(transpose ? Transpose(rows) : rows);
        if (oriented.Count == 0) return new ShapedGrid([], []);

        var width = oriented[0].Count;
        if (!firstRowIsHeader)
            return new ShapedGrid(Enumerable.Repeat(string.Empty, width).ToList(), oriented);

        var headers = oriented[0].Select(cell => cell.Text ?? string.Empty).ToList();
        var data = oriented.Skip(1).ToList();
        return new ShapedGrid(headers, data);
    }

    private IReadOnlyList<IReadOnlyList<WorkbookCell>> Transpose(IReadOnlyList<IReadOnlyList<WorkbookCell>> rows)
    {
        if (rows.Count == 0) return rows;
        var width = rows.Max(r => r.Count);
        var result = new List<IReadOnlyList<WorkbookCell>>(width);
        for (var column = 0; column < width; column++)
        {
            var newRow = new List<WorkbookCell>(rows.Count);
            foreach (var row in rows)
                newRow.Add(column < row.Count ? row[column] : new WorkbookCell(null, WorkbookCellKind.Blank));
            result.Add(newRow);
        }
        return result;
    }

    private IReadOnlyList<IReadOnlyList<WorkbookCell>> Rectangularize(IReadOnlyList<IReadOnlyList<WorkbookCell>> rows)
    {
        if (rows.Count == 0) return rows;
        var width = rows.Max(r => r.Count);
        if (rows.All(r => r.Count == width)) return rows;
        return rows
            .Select(row => (IReadOnlyList<WorkbookCell>)Enumerable.Range(0, width)
                .Select(i => i < row.Count ? row[i] : new WorkbookCell(null, WorkbookCellKind.Blank))
                .ToList())
            .ToList();
    }
}
