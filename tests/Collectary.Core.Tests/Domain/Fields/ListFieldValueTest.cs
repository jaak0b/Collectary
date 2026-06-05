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

    [Test]
    public void ReferencedBlobKeys_RecursesSubValuesAndIgnoresNonBlob()
    {
        var value = new ListFieldValue();
        value.Entries.Add(new ListEntry
        {
            SubValues =
            {
                new ImageFieldValue { ImageKey = "img" },
                new TextFieldValue { Value = "nope" },
            }
        });
        value.Entries.Add(new ListEntry { SubValues = { new AudioFieldValue { AudioKey = "aud" } } });

        Assert.That(value.ReferencedBlobKeys(), Is.EqualTo(new[] { "img", "aud" }));
        Assert.That(new ListFieldValue().ReferencedBlobKeys(), Is.Empty);
    }

    [Test]
    public void ReferencedBlobKeys_DefaultsToEmptyForNonBlobValue() =>
        Assert.That(new TextFieldValue { Value = "x" }.ReferencedBlobKeys(), Is.Empty);
}
