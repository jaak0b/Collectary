using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class BarcodeFieldValueTest
{
    [Test]
    public void IsEmpty_ForWhitespaceOrNullCode()
    {
        Assert.That(new BarcodeFieldValue { Code = " " }.IsEmpty, Is.True);
        Assert.That(new BarcodeFieldValue { Code = null }.IsEmpty, Is.True);
        Assert.That(new BarcodeFieldValue { Code = "5901234123457" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsCodeOrEmpty()
    {
        Assert.That(new BarcodeFieldValue { Code = "abc123" }.ToString(), Is.EqualTo("abc123"));
        Assert.That(new BarcodeFieldValue { Code = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesCodeAndSymbology()
    {
        var target = new BarcodeFieldValue();
        target.CopyFrom(new BarcodeFieldValue { Code = "5901234123457", Symbology = BarcodeSymbology.Ean13 });
        Assert.That(target.Code, Is.EqualTo("5901234123457"));
        Assert.That(target.Symbology, Is.EqualTo(BarcodeSymbology.Ean13));
    }

    [Test]
    public void Symbology_DefaultsToUnknown() =>
        Assert.That(new BarcodeFieldValue().Symbology, Is.EqualTo(BarcodeSymbology.Unknown));
}
