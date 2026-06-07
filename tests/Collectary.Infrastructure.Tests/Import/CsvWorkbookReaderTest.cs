using System.Text;
using Collectary.Core.Domain.Import;
using Collectary.Infrastructure.Import;

namespace Collectary.Infrastructure.Tests.Import;

[TestFixture]
public class CsvWorkbookReaderTest
{
    private MemoryStream Stream(string csv) => new(Encoding.UTF8.GetBytes(csv));

    private CsvWorkbookReader Reader() => new();

    [Test]
    public void Read_ProducesSingleSheet()
    {
        using var stream = Stream("Name,Pages\nDune,412");
        var data = Reader().Read(stream);
        Assert.That(data.Sheets, Has.Count.EqualTo(1));
        Assert.That(data.Sheets[0].Name, Is.EqualTo("CSV"));
    }

    [Test]
    public void Read_ParsesRowsAsTextCells()
    {
        using var stream = Stream("Name,Pages\nDune,412");
        var rows = Reader().Read(stream).Sheets[0].Rows;
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0][0].Text, Is.EqualTo("Name"));
        Assert.That(rows[0][0].Kind, Is.EqualTo(WorkbookCellKind.Text));
        Assert.That(rows[1][1].Text, Is.EqualTo("412"));
    }

    [Test]
    public void Read_EmptyField_IsBlank()
    {
        using var stream = Stream("a,,c");
        var row = Reader().Read(stream).Sheets[0].Rows[0];
        Assert.That(row[1].Kind, Is.EqualTo(WorkbookCellKind.Blank));
        Assert.That(row[1].Text, Is.Null);
    }

    [Test]
    public void Read_TagsInvariantNumbersAsNumber()
    {
        using var stream = Stream("Price\n1234.56");
        var cell = Reader().Read(stream).Sheets[0].Rows[1][0];
        Assert.That(cell.Kind, Is.EqualTo(WorkbookCellKind.Number));
        Assert.That(cell.Text, Is.EqualTo("1234.56"));
    }

    [Test]
    public void Read_TagsIsoDatesAsDateTime()
    {
        using var stream = Stream("When\n2024-12-31");
        var cell = Reader().Read(stream).Sheets[0].Rows[1][0];
        Assert.That(cell.Kind, Is.EqualTo(WorkbookCellKind.DateTime));
    }

    [Test]
    public void Read_KeepsLocaleFormattedNumbersAsText()
    {
        using var stream = Stream("Preis;Note\n1.234,56;x");
        var cell = Reader().Read(stream).Sheets[0].Rows[1][0];
        Assert.That(cell.Kind, Is.EqualTo(WorkbookCellKind.Text));
        Assert.That(cell.Text, Is.EqualTo("1.234,56"));
    }

    [Test]
    public void Read_HonoursSemicolonDelimiter()
    {
        using var stream = Stream("Name;Preis\nDune;1.234,56");
        var rows = Reader().Read(stream).Sheets[0].Rows;
        Assert.That(rows[1][0].Text, Is.EqualTo("Dune"));
        Assert.That(rows[1][1].Text, Is.EqualTo("1.234,56"));
    }

    [Test]
    public void Read_DecodesWindows1252WithoutBom()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.GetEncoding(1252).GetBytes("Train\nÖBB Railjet");
        using var stream = new MemoryStream(bytes);
        var rows = Reader().Read(stream).Sheets[0].Rows;
        Assert.That(rows[1][0].Text, Is.EqualTo("ÖBB Railjet"));
    }

    [Test]
    public void Read_DecodesUtf8WithBom()
    {
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes("Train\nÖBB Railjet");
        using var stream = new MemoryStream(bytes);
        var rows = Reader().Read(stream).Sheets[0].Rows;
        Assert.That(rows[0][0].Text, Is.EqualTo("Train"));
        Assert.That(rows[1][0].Text, Is.EqualTo("ÖBB Railjet"));
    }

    [Test]
    public void Read_DecodesPlainUtf8()
    {
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes("Train\nÖBB Railjet");
        using var stream = new MemoryStream(bytes);
        var rows = Reader().Read(stream).Sheets[0].Rows;
        Assert.That(rows[1][0].Text, Is.EqualTo("ÖBB Railjet"));
    }

    [Test]
    public void Read_DecodesUtf16LittleEndianWithBom()
    {
        var enc = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
        var bytes = enc.GetPreamble().Concat(enc.GetBytes("Train\nÖBB Railjet")).ToArray();
        using var stream = new MemoryStream(bytes);
        var rows = Reader().Read(stream).Sheets[0].Rows;
        Assert.That(rows[0][0].Text, Is.EqualTo("Train"));
        Assert.That(rows[1][0].Text, Is.EqualTo("ÖBB Railjet"));
    }

    [Test]
    public void Read_DecodesUtf16BigEndianWithBom()
    {
        var enc = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
        var bytes = enc.GetPreamble().Concat(enc.GetBytes("Train\nÖBB Railjet")).ToArray();
        using var stream = new MemoryStream(bytes);
        var rows = Reader().Read(stream).Sheets[0].Rows;
        Assert.That(rows[0][0].Text, Is.EqualTo("Train"));
        Assert.That(rows[1][0].Text, Is.EqualTo("ÖBB Railjet"));
    }

    [Test]
    public void Read_HandlesQuotedEmbeddedDelimiter()
    {
        using var stream = Stream("Name,Note\n\"Dune, the novel\",good");
        var row = Reader().Read(stream).Sheets[0].Rows[1];
        Assert.That(row[0].Text, Is.EqualTo("Dune, the novel"));
        Assert.That(row[1].Text, Is.EqualTo("good"));
    }
}
