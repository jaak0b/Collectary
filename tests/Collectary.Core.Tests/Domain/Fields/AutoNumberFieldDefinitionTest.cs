using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class AutoNumberFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new AutoNumberFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<AutoNumberFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void NextNumber_EmptySet_IsOne([Values(AutoNumberStrategy.HighestPlusOne, AutoNumberStrategy.FillGaps)] AutoNumberStrategy strategy)
    {
        var def = new AutoNumberFieldDefinition { Strategy = strategy };
        Assert.That(def.NextNumber(System.Array.Empty<int>()), Is.EqualTo(1));
    }

    [Test]
    public void NextNumber_HighestPlusOne_IgnoresGapsAndTakesMaxPlusOne()
    {
        var def = new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.HighestPlusOne };
        Assert.That(def.NextNumber(new[] { 1, 2, 5 }), Is.EqualTo(6));
    }

    [Test]
    public void NextNumber_FillGaps_TakesLowestFreeNumber()
    {
        var def = new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.FillGaps };
        Assert.Multiple(() =>
        {
            Assert.That(def.NextNumber(new[] { 1, 2, 4 }), Is.EqualTo(3));
            Assert.That(def.NextNumber(new[] { 1, 2, 3 }), Is.EqualTo(4));
            Assert.That(def.NextNumber(new[] { 2, 3 }), Is.EqualTo(1));
        });
    }

    [Test]
    public void ApplyTypeSpecificProperties_CopiesConfig()
    {
        var target = new AutoNumberFieldDefinition();
        target.ApplyTypeSpecificProperties(new AutoNumberFieldDefinition
        {
            Editable = true,
            Strategy = AutoNumberStrategy.FillGaps,
            OnDuplicate = DuplicateHandling.Warn,
        });
        Assert.Multiple(() =>
        {
            Assert.That(target.Editable, Is.True);
            Assert.That(target.Strategy, Is.EqualTo(AutoNumberStrategy.FillGaps));
            Assert.That(target.OnDuplicate, Is.EqualTo(DuplicateHandling.Warn));
        });
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new AutoNumberFieldDefinition { Editable = true, Strategy = AutoNumberStrategy.FillGaps };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.Multiple(() =>
        {
            Assert.That(target.Editable, Is.True);
            Assert.That(target.Strategy, Is.EqualTo(AutoNumberStrategy.FillGaps));
        });
    }

    [Test]
    public void Defaults_AreReadOnly_HighestPlusOne_ErrorOnDuplicate()
    {
        var def = new AutoNumberFieldDefinition();
        Assert.Multiple(() =>
        {
            Assert.That(def.Editable, Is.False);
            Assert.That(def.Strategy, Is.EqualTo(AutoNumberStrategy.HighestPlusOne));
            Assert.That(def.OnDuplicate, Is.EqualTo(DuplicateHandling.Error));
            Assert.That(def.ShowInList, Is.True);
        });
    }

    [Test]
    public void SearchSurface_ExposesComparisonOperatorsAndNoSuggestions()
    {
        ISearchableFieldDefinition search = new AutoNumberFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Equals));
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Greater));
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.IsEmpty));
        Assert.That(search.ValueSuggestions(), Is.Empty);
    }

    [Test]
    public void TryCreateMatcher_Equals_MatchesTheStoredNumber()
    {
        var def = new AutoNumberFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["7"], out var matcher, out _), Is.True);

        var hit = new Item { Values = [new AutoNumberFieldValue { FieldDefinitionId = def.Id, Value = 7 }] };
        var miss = new Item { Values = [new AutoNumberFieldValue { FieldDefinitionId = def.Id, Value = 8 }] };
        Assert.That(matcher!.Matches(hit, [def.Id]), Is.True);
        Assert.That(matcher.Matches(miss, [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_Greater_MatchesLargerStoredNumber()
    {
        var def = new AutoNumberFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["5"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new AutoNumberFieldValue { FieldDefinitionId = def.Id, Value = 6 }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        Assert.That(matcher.Matches(new Item(), [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_NonNumericOperand_ReportsInvalidValue()
    {
        ISearchableFieldDefinition search = new AutoNumberFieldDefinition();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["abc"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.InvalidValue));
    }

    [Test]
    public void SortKey_ReturnsTheStoredNumber()
    {
        ISearchableFieldDefinition search = new AutoNumberFieldDefinition();
        Assert.That(search.SortKey(new Item(), new AutoNumberFieldValue { Value = 7 }), Is.EqualTo(7));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
