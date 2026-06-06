using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class BoolFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new BoolFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<BoolFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void ThreeState_DefaultsToFalse() =>
        Assert.That(new BoolFieldDefinition().ThreeState, Is.False);

    [Test]
    public void ApplyTypeSpecificProperties_CopiesThreeState()
    {
        var target = new BoolFieldDefinition { ThreeState = false };
        target.ApplyTypeSpecificProperties(new BoolFieldDefinition { ThreeState = true });
        Assert.That(target.ThreeState, Is.True);
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new BoolFieldDefinition { ThreeState = true };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.That(target.ThreeState, Is.True);
    }

    [TestCase("true")]
    [TestCase("yes")]
    [TestCase("y")]
    [TestCase("1")]
    [TestCase("x")]
    [TestCase("✓")]
    [TestCase("ja")]
    [TestCase("wahr")]
    public void TryImportFromText_ParsesTruthyTokens(string raw)
    {
        var ok = ((ITextImportable)new BoolFieldDefinition()).TryImportFromText(raw, CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((BoolFieldValue)v).Value, Is.True);
    }

    [TestCase("false")]
    [TestCase("no")]
    [TestCase("n")]
    [TestCase("0")]
    [TestCase("nein")]
    [TestCase("falsch")]
    public void TryImportFromText_ParsesFalsyTokens(string raw)
    {
        var ok = ((ITextImportable)new BoolFieldDefinition()).TryImportFromText(raw, CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((BoolFieldValue)v).Value, Is.False);
    }

    [Test]
    public void TryImportFromText_RejectsUnknownToken()
    {
        var ok = ((ITextImportable)new BoolFieldDefinition()).TryImportFromText("maybe", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }
}
