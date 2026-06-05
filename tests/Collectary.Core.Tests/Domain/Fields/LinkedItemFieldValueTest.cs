using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class LinkedItemFieldValueTest
{
    [Test]
    public void IsEmpty_WhenNoTarget()
    {
        Assert.That(new LinkedItemFieldValue().IsEmpty, Is.True);
        Assert.That(new LinkedItemFieldValue { TargetItemId = Guid.NewGuid() }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsCachedDisplayOrEmpty()
    {
        Assert.That(new LinkedItemFieldValue { TargetDisplay = "Falcon" }.ToString(), Is.EqualTo("Falcon"));
        Assert.That(new LinkedItemFieldValue().ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesTargetAndDisplay()
    {
        var id = Guid.NewGuid();
        var target = new LinkedItemFieldValue();
        target.CopyFrom(new LinkedItemFieldValue { TargetItemId = id, TargetDisplay = "X-Wing" });
        Assert.That(target.TargetItemId, Is.EqualTo(id));
        Assert.That(target.TargetDisplay, Is.EqualTo("X-Wing"));
    }
}
