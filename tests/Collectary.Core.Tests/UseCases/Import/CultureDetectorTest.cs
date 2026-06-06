using System.Globalization;
using Collectary.Core.Domain.Import;
using Collectary.Core.UseCases.Import;

namespace Collectary.Core.Tests.UseCases.Import;

[TestFixture]
public class CultureDetectorTest
{
    private readonly CultureInfo _en = new("en-US");
    private readonly CultureInfo _de = new("de-DE");

    private WorkbookCell Text(string s) => new(s, WorkbookCellKind.Text);
    private WorkbookCell Number(string s) => new(s, WorkbookCellKind.Number);
    private IReadOnlyList<IReadOnlyList<WorkbookCell>> Grid(params IReadOnlyList<WorkbookCell>[] rows) => rows;

    [Test]
    public void Detect_PicksCultureThatParsesMostTextCells()
    {
        var grid = Grid(
            new[] { Text("1.234,56") },
            new[] { Text("31.12.2024") });

        var result = new CultureDetector().Detect(grid, new[] { _en, _de }, _en);

        Assert.That(result, Is.EqualTo(_de));
    }

    [Test]
    public void Detect_SingleGermanFormattedCell_PicksGermanOverFallback()
    {
        var grid = Grid(new[] { Text("1.234,56") });

        var result = new CultureDetector().Detect(grid, new[] { _de, _en }, _en);

        Assert.That(result, Is.EqualTo(_de));
    }

    [Test]
    public void Detect_TieBreaksTowardFallback()
    {
        var grid = Grid(
            new[] { Text("100") },
            new[] { Text("200") });

        var result = new CultureDetector().Detect(grid, new[] { _de, _en }, _en);

        Assert.That(result, Is.EqualTo(_en));
    }

    [Test]
    public void Detect_IgnoresTypedCells()
    {
        var grid = Grid(
            new[] { Number("1.234,56") },
            new[] { Number("9.999,11") });

        var result = new CultureDetector().Detect(grid, new[] { _de, _en }, _en);

        Assert.That(result, Is.EqualTo(_en));
    }

    [Test]
    public void Detect_NoCandidatesScores_ReturnsFallback()
    {
        var grid = Grid(new[] { Text("hello"), Text("world") });

        var result = new CultureDetector().Detect(grid, new[] { _de, _en }, _en);

        Assert.That(result, Is.EqualTo(_en));
    }
}
