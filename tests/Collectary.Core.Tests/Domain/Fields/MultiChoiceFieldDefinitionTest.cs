using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MultiChoiceFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_SplitsOnCommaAndSemicolon()
    {
        var ok = ((ITextImportable)new MultiChoiceFieldDefinition()).TryImportFromText("a, b; c", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((MultiChoiceFieldValue)v).Selected, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void TryImportFromText_RejectsAnyValueNotInDefinedChoices()
    {
        var def = new MultiChoiceFieldDefinition();
        def.Choices.Add(new ChoiceOption { Value = "Red" });
        def.Choices.Add(new ChoiceOption { Value = "Green" });
        var ok = ((ITextImportable)def).TryImportFromText("Red; Blue", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new MultiChoiceFieldDefinition()).TryImportFromText("  ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new MultiChoiceFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<MultiChoiceFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultColumnSpan_IsTwo() =>
        Assert.That(new MultiChoiceFieldDefinition().DefaultColumnSpan, Is.EqualTo(2));

    [Test]
    public void ApplyTypeSpecificProperties_ReplacesChoicesWithCopies()
    {
        var source = new MultiChoiceFieldDefinition();
        var original = new ChoiceOption { Value = "A", DisplayOrder = 3 };
        source.Choices.Add(original);

        var target = new MultiChoiceFieldDefinition();
        target.Choices.Add(new ChoiceOption { Value = "Stale" });
        target.ApplyTypeSpecificProperties(source);

        Assert.That(target.Choices, Has.Count.EqualTo(1));
        Assert.That(target.Choices[0].Value, Is.EqualTo("A"));
        Assert.That(target.Choices[0].DisplayOrder, Is.EqualTo(3));
        Assert.That(target.Choices[0].Id, Is.EqualTo(original.Id));
        Assert.That(target.Choices[0], Is.Not.SameAs(original));
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new MultiChoiceFieldDefinition();
        target.Choices.Add(new ChoiceOption { Value = "Keep" });
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.That(target.Choices, Has.Count.EqualTo(1));
        Assert.That(target.Choices[0].Value, Is.EqualTo("Keep"));
    }
}
