using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

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
    public void DisplayMode_DefaultsToExpanded() =>
        Assert.That(new MultiChoiceFieldDefinition().DisplayMode, Is.EqualTo(MultiChoiceDisplayMode.Expanded));

    [Test]
    public void ApplyTypeSpecificProperties_CopiesDisplayMode()
    {
        var source = new MultiChoiceFieldDefinition { DisplayMode = MultiChoiceDisplayMode.Collapsed };
        var target = new MultiChoiceFieldDefinition();
        target.ApplyTypeSpecificProperties(source);
        Assert.That(target.DisplayMode, Is.EqualTo(MultiChoiceDisplayMode.Collapsed));
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

    [Test]
    public void TryCreateMatcher_Equals_MatchesAnySelectedEntry()
    {
        var def = new MultiChoiceFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["red"], out var matcher, out _), Is.True);

        var item = new Item
        {
            Values = [new MultiChoiceFieldValue { FieldDefinitionId = def.Id, Selected = ["Red", "Green"] }],
        };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        ((MultiChoiceFieldValue)item.Values[0]).Selected = ["Green"];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void ValueSuggestions_ListChoicesInDisplayOrder()
    {
        var def = new MultiChoiceFieldDefinition();
        def.Choices.Add(new ChoiceOption { Value = "B", DisplayOrder = 2 });
        def.Choices.Add(new ChoiceOption { Value = "A", DisplayOrder = 1 });

        ISearchableFieldDefinition search = def;
        Assert.That(search.ValueSuggestions(), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void SearchSurface_ExposesOperatorsAndSortKey()
    {
        ISearchableFieldDefinition search = new MultiChoiceFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Contains));
        Assert.That(search.SortKey(new Item(), new MultiChoiceFieldValue { Selected = ["a", "b"] }), Is.EqualTo("a, b"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
