using System.Globalization;
using ClosedXML.Excel;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Import;

public sealed class ClosedXmlWorkbookReader : IExcelWorkbookReader
{
    public WorkbookData Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheets = new List<WorkbookSheet>();
        foreach (var worksheet in workbook.Worksheets)
        {
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            var rows = new List<IReadOnlyList<WorkbookCell>>(lastRow);
            for (var row = 1; row <= lastRow; row++)
            {
                var cells = new List<WorkbookCell>(lastColumn);
                for (var column = 1; column <= lastColumn; column++)
                    cells.Add(ReadCell(worksheet.Cell(row, column)));
                rows.Add(cells);
            }
            sheets.Add(new WorkbookSheet(worksheet.Name, rows));
        }
        return new WorkbookData(sheets);
    }

    private WorkbookCell ReadCell(IXLCell cell)
    {
        if (cell.IsEmpty()) return new WorkbookCell(null, WorkbookCellKind.Blank);
        return cell.DataType switch
        {
            XLDataType.Number => new WorkbookCell(cell.GetDouble().ToString(CultureInfo.InvariantCulture), WorkbookCellKind.Number),
            XLDataType.DateTime => new WorkbookCell(cell.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), WorkbookCellKind.DateTime),
            XLDataType.Boolean => new WorkbookCell(cell.GetBoolean() ? "true" : "false", WorkbookCellKind.Boolean),
            XLDataType.TimeSpan => new WorkbookCell(cell.GetTimeSpan().ToString(), WorkbookCellKind.Text),
            XLDataType.Error => new WorkbookCell(null, WorkbookCellKind.Blank),
            _ => new WorkbookCell(cell.GetString(), WorkbookCellKind.Text)
        };
    }
}
