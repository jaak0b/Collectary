using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class LinkedItemFieldDefinitionTest
{
    [Test]
    public void IsNotTextImportable() =>
        Assert.That(new LinkedItemFieldDefinition() is ITextImportable, Is.False);

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new LinkedItemFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<LinkedItemFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new LinkedItemFieldDefinition(), Is.InstanceOf<IListDisplayable>());

    [Test]
    public void TryCreateMatcher_Contains_MatchesTargetDisplayText()
    {
        var def = new LinkedItemFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Contains, ["loco"], out var matcher, out _), Is.True);

        var item = new Item
        {
            Values = [new LinkedItemFieldValue
            {
                FieldDefinitionId = def.Id,
                TargetItemId = Guid.NewGuid(),
                TargetDisplay = "Loco 42",
            }],
        };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        ((LinkedItemFieldValue)item.Values[0]).TargetDisplay = "Wagon";
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new LinkedItemFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Equals));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new LinkedItemFieldValue { TargetDisplay = "Loco" }), Is.EqualTo("Loco"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
