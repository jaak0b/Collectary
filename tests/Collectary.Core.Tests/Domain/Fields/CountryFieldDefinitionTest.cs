using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class CountryFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_UppercasesTwoLetterCode()
    {
        var ok = ((ITextImportable)new CountryFieldDefinition()).TryImportFromText("de", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((CountryFieldValue)v).Code, Is.EqualTo("DE"));
    }

    [Test]
    public void TryImportFromText_RejectsNonCode()
    {
        var ok = ((ITextImportable)new CountryFieldDefinition()).TryImportFromText("Germany", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new CountryFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<CountryFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new CountryFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
