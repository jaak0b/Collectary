using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class RatingFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_ParsesStarsWithinRange()
    {
        var ok = ((ITextImportable)new RatingFieldDefinition { MaxStars = 5 }).TryImportFromText("4", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((RatingFieldValue)v).Stars, Is.EqualTo(4));
    }

    [Test]
    public void TryImportFromText_RejectsOutOfRange()
    {
        var ok = ((ITextImportable)new RatingFieldDefinition { MaxStars = 5 }).TryImportFromText("9", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryImportFromText_RejectsNonNumber()
    {
        var ok = ((ITextImportable)new RatingFieldDefinition()).TryImportFromText("great", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new RatingFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<RatingFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultsToFiveStars() =>
        Assert.That(new RatingFieldDefinition().MaxStars, Is.EqualTo(5));
}
