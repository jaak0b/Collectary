using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DisplayNameFieldDefinitionTest
{
    [Test]
    public void IsNotTextImportable() =>
        Assert.That(new DisplayNameFieldDefinition() is ITextImportable, Is.False);

    [Test]
    public void ValueType_IsTextFieldValue() =>
        Assert.That(new DisplayNameFieldDefinition().ValueType, Is.EqualTo(typeof(TextFieldValue)));

    [Test]
    public void ShowInList_DefaultsToTrue() =>
        Assert.That(new DisplayNameFieldDefinition().ShowInList, Is.True);

    [Test]
    public void CreateEmptyValue_Throws() =>
        Assert.Throws<NotSupportedException>(() => new DisplayNameFieldDefinition().CreateEmptyValue());

    [Test]
    public void IsTitleField_IsTrue() =>
        Assert.That(new DisplayNameFieldDefinition().IsTitleField, Is.True);

    [Test]
    public void IsTitleField_IsFalse_ForNonTitleType() =>
        Assert.That(new TextFieldDefinition().IsTitleField, Is.False);

    [Test]
    public void TryCreateMatcher_Equals_MatchesItemDisplayName()
    {
        var def = new DisplayNameFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["loco"], out var matcher, out _), Is.True);

        Assert.That(matcher!.Matches(new Item { DisplayName = "LOCO" }, [def.Id]), Is.True);
        Assert.That(matcher.Matches(new Item { DisplayName = "Wagon" }, [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_UnsupportedOperator_ReportsOperatorNotSupported()
    {
        ISearchableFieldDefinition search = new DisplayNameFieldDefinition();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["1"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
    }

    [Test]
    public void SortKey_ReturnsItemDisplayName()
    {
        ISearchableFieldDefinition search = new DisplayNameFieldDefinition();
        Assert.That(search.SortKey(new Item { DisplayName = "Loco" }, null), Is.EqualTo("Loco"));
    }

    [Test]
    public void SearchSurface_ExposesNameOperatorsAndNoSuggestions()
    {
        ISearchableFieldDefinition search = new DisplayNameFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Contains));
        Assert.That(search.SupportedOperators, Does.Not.Contain(QueryOperatorKind.Greater));
        Assert.That(search.ValueSuggestions(), Is.Empty);
    }
}
