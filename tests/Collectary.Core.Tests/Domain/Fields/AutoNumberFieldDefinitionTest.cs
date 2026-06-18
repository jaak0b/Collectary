using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

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
    public void NextNumber_HighestPlusOne_AtIntMaxValue_DoesNotOverflowToNegative()
    {
        var def = new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.HighestPlusOne };
        var used = new[] { 1, 2, int.MaxValue };

        var next = def.NextNumber(used);

        Assert.Multiple(() =>
        {
            Assert.That(next, Is.GreaterThan(0), "the next number must never wrap to a negative value");
            Assert.That(used, Does.Not.Contain(next), "the next number must not collide with an existing one");
        });
    }

    [Test]
    public void EnforcesUniqueImportValues_IsTrue_WhenDuplicatesAreErrorOrWarn(
        [Values(DuplicateHandling.Error, DuplicateHandling.Warn)] DuplicateHandling mode)
    {
        Assert.That(new AutoNumberFieldDefinition { OnDuplicate = mode }.EnforcesUniqueImportValues, Is.True);
    }

    [Test]
    public void EnforcesUniqueImportValues_IsFalse_WhenDuplicatesAreAllowed()
    {
        Assert.That(new AutoNumberFieldDefinition { OnDuplicate = DuplicateHandling.Allow }.EnforcesUniqueImportValues, Is.False);
    }

    [Test]
    public void EnforcesUniqueImportValues_IsFalse_ByDefaultForImportableFieldsThatDoNotTrackUniqueness()
    {
        Assert.That(((ITextImportable)new TextFieldDefinition()).EnforcesUniqueImportValues, Is.False);
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
    public void TryImportFromText_ParsesInteger_IntoAutoNumberValue()
    {
        var def = new AutoNumberFieldDefinition();
        ITextImportable importable = def;

        Assert.That(importable.TryImportFromText("42", CultureInfo.InvariantCulture, out var value), Is.True);
        var auto = (AutoNumberFieldValue)value;
        Assert.That(auto.Value, Is.EqualTo(42));
        Assert.That(auto.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryImportFromText_AllowsThousandsSeparators()
    {
        ITextImportable importable = new AutoNumberFieldDefinition();

        Assert.That(importable.TryImportFromText("1,000", CultureInfo.InvariantCulture, out var value), Is.True);
        Assert.That(((AutoNumberFieldValue)value).Value, Is.EqualTo(1000));
    }

    [Test]
    public void TryImportFromText_NonInteger_ReturnsFalseWithEmptyValue()
    {
        ITextImportable importable = new AutoNumberFieldDefinition();

        Assert.That(importable.TryImportFromText("abc", CultureInfo.InvariantCulture, out var value), Is.False);
        Assert.That(value.IsEmpty, Is.True);
    }

    [Test]
    public void ImportInferenceOrder_IsMaxValue_SoAPlainNumberColumnNeverInfersAsAutoNumber()
    {
        Assert.That(((ITextImportable)new AutoNumberFieldDefinition()).ImportInferenceOrder, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void ApplyImportDefaults_MakesItEditableAndWarnOnDuplicate_SoImportedNumbersDoNotBlockSaving()
    {
        var def = new AutoNumberFieldDefinition();

        ((ITextImportable)def).ApplyImportDefaults();

        Assert.Multiple(() =>
        {
            Assert.That(def.Editable, Is.True);
            Assert.That(def.OnDuplicate, Is.EqualTo(DuplicateHandling.Warn));
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
