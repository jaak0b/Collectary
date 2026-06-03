using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class UrlFieldValueTest
{
    [Test]
    public void IsEmpty_ForWhitespace()
    {
        Assert.That(new UrlFieldValue { Url = "  " }.IsEmpty, Is.True);
        Assert.That(new UrlFieldValue { Url = "http://x" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsUrlOrEmpty()
    {
        Assert.That(new UrlFieldValue { Url = "http://x" }.ToString(), Is.EqualTo("http://x"));
        Assert.That(new UrlFieldValue { Url = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesUrl()
    {
        var target = new UrlFieldValue();
        target.CopyFrom(new UrlFieldValue { Url = "https://a.b" });
        Assert.That(target.Url, Is.EqualTo("https://a.b"));
    }
}
