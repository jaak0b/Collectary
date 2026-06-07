using System.Globalization;
using Collectary.Core.Domain.Import;

namespace Collectary.Core.Tests.Domain.Import;

[TestFixture]
public class WorkbookCellTest
{
    private readonly CultureInfo _de = new("de-DE");

    [Test]
    public void EffectiveCulture_TypedKinds_AreInvariant()
    {
        Assert.That(new WorkbookCell("1", WorkbookCellKind.Number).EffectiveCulture(_de), Is.SameAs(CultureInfo.InvariantCulture));
        Assert.That(new WorkbookCell("d", WorkbookCellKind.DateTime).EffectiveCulture(_de), Is.SameAs(CultureInfo.InvariantCulture));
        Assert.That(new WorkbookCell("true", WorkbookCellKind.Boolean).EffectiveCulture(_de), Is.SameAs(CultureInfo.InvariantCulture));
    }

    [Test]
    public void EffectiveCulture_TextKind_UsesGivenCulture()
    {
        Assert.That(new WorkbookCell("x", WorkbookCellKind.Text).EffectiveCulture(_de), Is.SameAs(_de));
    }
}
