using Collectary.Core.Domain.Import;
using Collectary.Core.UseCases.Import;

namespace Collectary.Core.Tests.UseCases.Import;

[TestFixture]
public class GridShaperTest
{
    private WorkbookCell Text(string s) => new(s, WorkbookCellKind.Text);
    private WorkbookCell Number(string s) => new(s, WorkbookCellKind.Number);
    private WorkbookCell Blank() => new(null, WorkbookCellKind.Blank);

    private IReadOnlyList<IReadOnlyList<WorkbookCell>> Grid(params IReadOnlyList<WorkbookCell>[] rows) => rows;

    [Test]
    public void Shape_HeaderRow_SplitsHeadersFromData()
    {
        var grid = Grid(
            new[] { Text("Title"), Text("Pages") },
            new[] { Text("Dune"), Number("412") });

        var shaped = new GridShaper().Shape(grid, transpose: false, firstRowIsHeader: true);

        Assert.That(shaped.Headers, Is.EqualTo(new[] { "Title", "Pages" }));
        Assert.That(shaped.Rows, Has.Count.EqualTo(1));
        Assert.That(shaped.Rows[0][0].Text, Is.EqualTo("Dune"));
        Assert.That(shaped.Rows[0][1].Text, Is.EqualTo("412"));
    }

    [Test]
    public void Shape_NoHeader_LeavesHeadersBlankAndKeepsAllRows()
    {
        var grid = Grid(
            new[] { Text("Dune"), Number("412") },
            new[] { Text("Hobbit"), Number("310") });

        var shaped = new GridShaper().Shape(grid, transpose: false, firstRowIsHeader: false);

        Assert.That(shaped.Headers, Has.Count.EqualTo(2));
        Assert.That(shaped.Headers, Is.All.Empty);
        Assert.That(shaped.Rows, Has.Count.EqualTo(2));
    }

    [Test]
    public void Shape_Transpose_SwapsRowsAndColumns()
    {
        var grid = Grid(
            new[] { Text("a"), Text("b"), Text("c") },
            new[] { Text("d"), Text("e"), Text("f") });

        var shaped = new GridShaper().Shape(grid, transpose: true, firstRowIsHeader: false);

        Assert.That(shaped.Rows, Has.Count.EqualTo(3));
        Assert.That(shaped.Rows[0][0].Text, Is.EqualTo("a"));
        Assert.That(shaped.Rows[0][1].Text, Is.EqualTo("d"));
        Assert.That(shaped.Rows[2][0].Text, Is.EqualTo("c"));
        Assert.That(shaped.Rows[2][1].Text, Is.EqualTo("f"));
    }

    [Test]
    public void Shape_TransposeThenHeader_TakesHeadersFromTransposedFirstRow()
    {
        var grid = Grid(
            new[] { Text("Title"), Text("Dune") },
            new[] { Text("Pages"), Number("412") });

        var shaped = new GridShaper().Shape(grid, transpose: true, firstRowIsHeader: true);

        Assert.That(shaped.Headers, Is.EqualTo(new[] { "Title", "Pages" }));
        Assert.That(shaped.Rows, Has.Count.EqualTo(1));
        Assert.That(shaped.Rows[0][0].Text, Is.EqualTo("Dune"));
        Assert.That(shaped.Rows[0][1].Text, Is.EqualTo("412"));
    }

    [Test]
    public void Shape_BlankHeaderCell_YieldsEmptyHeader()
    {
        var grid = Grid(
            new[] { Text("Title"), Blank() },
            new[] { Text("Dune"), Text("x") });

        var shaped = new GridShaper().Shape(grid, transpose: false, firstRowIsHeader: true);

        Assert.That(shaped.Headers[0], Is.EqualTo("Title"));
        Assert.That(shaped.Headers[1], Is.Empty);
    }

    [Test]
    public void Shape_JaggedRows_PadShorterRowsToMaxWidth()
    {
        var grid = Grid(
            new[] { Text("a"), Text("b"), Text("c") },
            new[] { Text("d"), Text("e") });

        var shaped = new GridShaper().Shape(grid, transpose: false, firstRowIsHeader: false);

        Assert.That(shaped.Headers, Has.Count.EqualTo(3));
        Assert.That(shaped.Rows[1], Has.Count.EqualTo(3));
        Assert.That(shaped.Rows[1][2].Kind, Is.EqualTo(WorkbookCellKind.Blank));
    }

    [Test]
    public void Shape_TransposeJaggedRows_UsesMaxWidthAndPadsBlanks()
    {
        var grid = Grid(
            new[] { Text("a"), Text("b"), Text("c") },
            new[] { Text("d"), Text("e") });

        var shaped = new GridShaper().Shape(grid, transpose: true, firstRowIsHeader: false);

        Assert.That(shaped.Rows, Has.Count.EqualTo(3));
        Assert.That(shaped.Rows[2][0].Text, Is.EqualTo("c"));
        Assert.That(shaped.Rows[2][1].Kind, Is.EqualTo(WorkbookCellKind.Blank));
    }

    [Test]
    public void Shape_EmptyGrid_ReturnsEmpty()
    {
        var shaped = new GridShaper().Shape(Grid(), transpose: false, firstRowIsHeader: true);
        Assert.That(shaped.Headers, Is.Empty);
        Assert.That(shaped.Rows, Is.Empty);
    }
}
