using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class SingleChoiceFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_StoresSelectedValue()
    {
        var ok = ((ITextImportable)new SingleChoiceFieldDefinition()).TryImportFromText("Option A", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((SingleChoiceFieldValue)v).Selected, Is.EqualTo("Option A"));
    }

    [Test]
    public void TryImportFromText_RejectsValueNotInDefinedChoices()
    {
        var def = new SingleChoiceFieldDefinition();
        def.Choices.Add(new ChoiceOption { Value = "Red" });
        def.Choices.Add(new ChoiceOption { Value = "Green" });
        var ok = ((ITextImportable)def).TryImportFromText("Blue", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryImportFromText_AcceptsValueInDefinedChoicesIgnoringCase()
    {
        var def = new SingleChoiceFieldDefinition();
        def.Choices.Add(new ChoiceOption { Value = "Red" });
        var ok = ((ITextImportable)def).TryImportFromText("red", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((SingleChoiceFieldValue)v).Selected, Is.EqualTo("red"));
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new SingleChoiceFieldDefinition()).TryImportFromText("  ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new SingleChoiceFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<SingleChoiceFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void ValueSuggestions_ListChoicesInDisplayOrder()
    {
        var def = new SingleChoiceFieldDefinition();
        def.Choices.Add(new ChoiceOption { Value = "Closed", DisplayOrder = 2 });
        def.Choices.Add(new ChoiceOption { Value = "Open", DisplayOrder = 1 });

        ISearchableFieldDefinition search = def;
        Assert.That(search.ValueSuggestions(), Is.EqualTo(new[] { "Open", "Closed" }));
    }

    [Test]
    public void TryCreateMatcher_Equals_MatchesSelectedChoice()
    {
        var def = new SingleChoiceFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["open"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new SingleChoiceFieldValue { FieldDefinitionId = def.Id, Selected = "Open" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new SingleChoiceFieldValue { FieldDefinitionId = def.Id, Selected = "Closed" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsAndSortKey()
    {
        ISearchableFieldDefinition search = new SingleChoiceFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.In));
        Assert.That(search.SortKey(new Item(), new SingleChoiceFieldValue { Selected = "Open" }), Is.EqualTo("Open"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
