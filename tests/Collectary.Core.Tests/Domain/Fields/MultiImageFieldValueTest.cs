using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MultiImageFieldValueTest
{
    [Test]
    public void IsEmpty_WhenNoKeys()
    {
        Assert.That(new MultiImageFieldValue().IsEmpty, Is.True);
        Assert.That(new MultiImageFieldValue { ImageKeys = ["a"] }.IsEmpty, Is.False);
    }

    [Test]
    public void CopyFrom_CopiesKeysAsIndependentList()
    {
        var source = new MultiImageFieldValue { ImageKeys = ["a", "b"] };
        var target = new MultiImageFieldValue();

        target.CopyFrom(source);
        source.ImageKeys.Add("c");

        Assert.That(target.ImageKeys, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void ToString_ReportsCount()
    {
        Assert.That(new MultiImageFieldValue { ImageKeys = ["a", "b"] }.ToString(), Is.EqualTo("2"));
        Assert.That(new MultiImageFieldValue().ToString(), Is.EqualTo("0"));
    }
}
