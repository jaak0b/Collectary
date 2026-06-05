using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class FileAttachmentFieldValueTest
{
    [Test]
    public void IsEmpty_WhenNoFiles()
    {
        Assert.That(new FileAttachmentFieldValue().IsEmpty, Is.True);
        Assert.That(new FileAttachmentFieldValue { Files = [new("k", "manual.pdf")] }.IsEmpty, Is.False);
    }

    [Test]
    public void CopyFrom_CopiesFilesAsIndependentList()
    {
        var source = new FileAttachmentFieldValue { Files = [new("k1", "a.pdf")] };
        var target = new FileAttachmentFieldValue();

        target.CopyFrom(source);
        source.Files.Add(new("k2", "b.pdf"));

        Assert.That(target.Files, Has.Count.EqualTo(1));
        Assert.That(target.Files[0].FileName, Is.EqualTo("a.pdf"));
    }

    [Test]
    public void ToString_ReportsCount() =>
        Assert.That(new FileAttachmentFieldValue { Files = [new("k", "a"), new("k2", "b")] }.ToString(), Is.EqualTo("2"));

    [Test]
    public void ReferencedBlobKeys_ReturnsFileKeys()
    {
        Assert.That(new FileAttachmentFieldValue { Files = [new("k1", "a.pdf"), new("k2", "b.pdf")] }.ReferencedBlobKeys(),
            Is.EqualTo(new[] { "k1", "k2" }));
        Assert.That(new FileAttachmentFieldValue().ReferencedBlobKeys(), Is.Empty);
    }
}
