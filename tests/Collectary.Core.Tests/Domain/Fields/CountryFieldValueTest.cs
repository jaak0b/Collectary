using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class CountryFieldValueTest
{
    [Test]
    public void IsEmpty_ForWhitespaceOrNull()
    {
        Assert.That(new CountryFieldValue { Code = " " }.IsEmpty, Is.True);
        Assert.That(new CountryFieldValue { Code = null }.IsEmpty, Is.True);
        Assert.That(new CountryFieldValue { Code = "DE" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsCodeOrEmpty()
    {
        Assert.That(new CountryFieldValue { Code = "US" }.ToString(), Is.EqualTo("US"));
        Assert.That(new CountryFieldValue { Code = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesCode()
    {
        var target = new CountryFieldValue();
        target.CopyFrom(new CountryFieldValue { Code = "FR" });
        Assert.That(target.Code, Is.EqualTo("FR"));
    }
}
