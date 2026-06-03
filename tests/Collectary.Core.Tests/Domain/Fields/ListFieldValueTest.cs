using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ListFieldValueTest
{
    [Test]
    public void IsEmpty_WhenNoEntries()
    {
        var value = new ListFieldValue();
        Assert.That(value.IsEmpty, Is.True);
        value.Entries.Add(new ListEntry());
        Assert.That(value.IsEmpty, Is.False);
    }

    [Test]
    public void CopyFrom_CopiesEntries()
    {
        var source = new ListFieldValue();
        source.Entries.Add(new ListEntry());
        source.Entries.Add(new ListEntry());

        var target = new ListFieldValue();
        target.CopyFrom(source);

        Assert.That(target.Entries, Has.Count.EqualTo(2));
    }
}
