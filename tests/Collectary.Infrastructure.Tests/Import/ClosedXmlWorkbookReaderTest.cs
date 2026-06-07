using ClosedXML.Excel;
using Collectary.Core.Domain.Import;
using Collectary.Infrastructure.Import;

namespace Collectary.Infrastructure.Tests.Import;

[TestFixture]
public class ClosedXmlWorkbookReaderTest
{
    private MemoryStream BuildWorkbook(Action<IXLWorksheet> populate, string sheetName = "Books")
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add(sheetName);
            populate(ws);
            workbook.SaveAs(stream);
        }
        stream.Position = 0;
        return stream;
    }

    [Test]
    public void Read_ReturnsWorksheetNames()
    {
        using var stream = BuildWorkbook(ws => ws.Cell(1, 1).Value = "x", "Inventory");
        var data = new ClosedXmlWorkbookReader().Read(stream);
        Assert.That(data.Sheets, Has.Count.EqualTo(1));
        Assert.That(data.Sheets[0].Name, Is.EqualTo("Inventory"));
    }

    [Test]
    public void Read_PreservesTextCellsVerbatim()
    {
        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "Title";
            ws.Cell(2, 1).Value = "Dune";
        });
        var rows = new ClosedXmlWorkbookReader().Read(stream).Sheets[0].Rows;
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0][0].Text, Is.EqualTo("Title"));
        Assert.That(rows[0][0].Kind, Is.EqualTo(WorkbookCellKind.Text));
        Assert.That(rows[1][0].Text, Is.EqualTo("Dune"));
    }

    [Test]
    public void Read_EmitsNumbersAsInvariantStrings()
    {
        using var stream = BuildWorkbook(ws => ws.Cell(1, 1).Value = 1234.56);
        var cell = new ClosedXmlWorkbookReader().Read(stream).Sheets[0].Rows[0][0];
        Assert.That(cell.Kind, Is.EqualTo(WorkbookCellKind.Number));
        Assert.That(cell.Text, Is.EqualTo("1234.56"));
    }

    [Test]
    public void Read_EmitsDatesAsInvariantStrings()
    {
        using var stream = BuildWorkbook(ws => ws.Cell(1, 1).Value = new DateTime(1965, 8, 1));
        var cell = new ClosedXmlWorkbookReader().Read(stream).Sheets[0].Rows[0][0];
        Assert.That(cell.Kind, Is.EqualTo(WorkbookCellKind.DateTime));
        Assert.That(cell.Text, Does.StartWith("1965-08-01"));
    }

    [Test]
    public void Read_EmitsBooleans()
    {
        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = true;
            ws.Cell(2, 1).Value = false;
        });
        var rows = new ClosedXmlWorkbookReader().Read(stream).Sheets[0].Rows;
        Assert.That(rows[0][0].Kind, Is.EqualTo(WorkbookCellKind.Boolean));
        Assert.That(rows[0][0].Text, Is.EqualTo("true"));
        Assert.That(rows[1][0].Text, Is.EqualTo("false"));
    }

    [Test]
    public void Read_TreatsErrorCellsAsBlank()
    {
        using var stream = BuildWorkbook(ws => ws.Cell(1, 1).Value = XLError.DivisionByZero);
        var cell = new ClosedXmlWorkbookReader().Read(stream).Sheets[0].Rows[0][0];
        Assert.That(cell.Kind, Is.EqualTo(WorkbookCellKind.Blank));
        Assert.That(cell.Text, Is.Null);
    }

    [Test]
    public void Read_EmitsTimeSpanCellsAsText()
    {
        using var stream = BuildWorkbook(ws => ws.Cell(1, 1).Value = TimeSpan.FromMinutes(90));
        var cell = new ClosedXmlWorkbookReader().Read(stream).Sheets[0].Rows[0][0];
        Assert.That(cell.Kind, Is.EqualTo(WorkbookCellKind.Text));
        Assert.That(cell.Text, Is.EqualTo("01:30:00"));
    }

    [Test]
    public void Read_TagsEmptyCellsAsBlank()
    {
        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "A";
            ws.Cell(1, 3).Value = "C";
        });
        var row = new ClosedXmlWorkbookReader().Read(stream).Sheets[0].Rows[0];
        Assert.That(row, Has.Count.EqualTo(3));
        Assert.That(row[1].Kind, Is.EqualTo(WorkbookCellKind.Blank));
        Assert.That(row[1].Text, Is.Null);
    }
}
