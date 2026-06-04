using Collectary.Presentation.Layout;

namespace Collectary.UI.Tests.Layout;

[TestFixture]
public class FieldLayoutEngineTest
{
    [TestCase(4, 720.0, 180.0, 4)]
    [TestCase(4, 700.0, 180.0, 3)]
    [TestCase(4, 540.0, 180.0, 3)]
    [TestCase(4, 360.0, 180.0, 2)]
    [TestCase(4, 180.0, 180.0, 1)]
    [TestCase(1, 1000.0, 180.0, 1)]
    [TestCase(2, 350.0, 180.0, 1)]
    public void ComputeEffectiveCols_ReturnsCorrectCount(int desired, double width, double minW, int expected)
    {
        Assert.That(FieldLayoutEngine.ComputeEffectiveCols(desired, width, minW), Is.EqualTo(expected));
    }

    [Test]
    public void PackRows_TwoSpan1Fields_ShareOneRow()
    {
        var fields = new[] { (0, 1), (1, 1) };
        var rows = FieldLayoutEngine.PackRows(fields, effectiveCols: 2);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Slots, Has.Count.EqualTo(2));
        Assert.That(rows[0].Slots[0].ColStart, Is.EqualTo(0));
        Assert.That(rows[0].Slots[1].ColStart, Is.EqualTo(1));
    }

    [Test]
    public void PackRows_Span2FieldAlone_TakesFullRow()
    {
        var fields = new[] { (0, 2) };
        var rows = FieldLayoutEngine.PackRows(fields, effectiveCols: 2);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Slots[0].Span, Is.EqualTo(2));
        Assert.That(rows[0].Slots[0].ColStart, Is.EqualTo(0));
    }

    [Test]
    public void PackRows_Span1ThenSpan2_GoesToNewRow()
    {
        var fields = new[] { (0, 1), (1, 2) };
        var rows = FieldLayoutEngine.PackRows(fields, effectiveCols: 2);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].Slots[0].FieldIndex, Is.EqualTo(0));
        Assert.That(rows[1].Slots[0].FieldIndex, Is.EqualTo(1));
    }

    [Test]
    public void PackRows_3Fields2Cols_CorrectRows()
    {
        var fields = new[] { (0, 1), (1, 1), (2, 2) };
        var rows = FieldLayoutEngine.PackRows(fields, effectiveCols: 2);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].Slots, Has.Count.EqualTo(2));
        Assert.That(rows[1].Slots[0].Span, Is.EqualTo(2));
    }

    [Test]
    public void PackRows_Span2ClampedToEffectiveCols_WhenCols1()
    {
        var fields = new[] { (0, 2), (1, 1) };
        var rows = FieldLayoutEngine.PackRows(fields, effectiveCols: 1);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].Slots[0].Span, Is.EqualTo(1));
        Assert.That(rows[1].Slots[0].Span, Is.EqualTo(1));
    }

    [Test]
    public void PackRows_EmptyFields_ReturnsEmpty()
    {
        var rows = FieldLayoutEngine.PackRows(Array.Empty<(int, int)>(), effectiveCols: 2);
        Assert.That(rows, Is.Empty);
    }

    [Test]
    public void PackRows_Span3With4Cols_FitsInOneRow()
    {
        var fields = new[] { (0, 3), (1, 1) };
        var rows = FieldLayoutEngine.PackRows(fields, effectiveCols: 4);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Slots[0].Span, Is.EqualTo(3));
        Assert.That(rows[0].Slots[1].Span, Is.EqualTo(1));
        Assert.That(rows[0].Slots[1].ColStart, Is.EqualTo(3));
    }

    [Test]
    public void PackRows_Span4With4Cols_TakesFullRow()
    {
        var fields = new[] { (0, 4), (1, 1) };
        var rows = FieldLayoutEngine.PackRows(fields, effectiveCols: 4);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].Slots[0].Span, Is.EqualTo(4));
        Assert.That(rows[1].Slots[0].Span, Is.EqualTo(1));
    }
}
