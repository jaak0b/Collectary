using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class QrCodeFieldValueTest
{
    [Test]
    public void IsEmpty_ForWhitespaceOrNull()
    {
        Assert.That(new QrCodeFieldValue { Content = " " }.IsEmpty, Is.True);
        Assert.That(new QrCodeFieldValue { Content = null }.IsEmpty, Is.True);
        Assert.That(new QrCodeFieldValue { Content = "https://collectary.app/i/7" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsContentOrEmpty()
    {
        Assert.That(new QrCodeFieldValue { Content = "SHELF-A1" }.ToString(), Is.EqualTo("SHELF-A1"));
        Assert.That(new QrCodeFieldValue { Content = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesContent()
    {
        var target = new QrCodeFieldValue();
        target.CopyFrom(new QrCodeFieldValue { Content = "BOX-42" });
        Assert.That(target.Content, Is.EqualTo("BOX-42"));
    }
}
