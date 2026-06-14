using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class RichTextFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_StoresText()
    {
        var ok = ((ITextImportable)new RichTextFieldDefinition()).TryImportFromText("<b>hi</b>", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((RichTextFieldValue)v).Value, Is.EqualTo("<b>hi</b>"));
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new RichTextFieldDefinition()).TryImportFromText("  ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void ImportInferenceOrder_IsLast() =>
        Assert.That(((ITextImportable)new RichTextFieldDefinition()).ImportInferenceOrder, Is.EqualTo(int.MaxValue));

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new RichTextFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<RichTextFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryCreateMatcher_Contains_MatchesInsideMarkup()
    {
        var def = new RichTextFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Contains, ["bold"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new RichTextFieldValue { FieldDefinitionId = def.Id, Value = "<b>Bold</b>" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new RichTextFieldValue { FieldDefinitionId = def.Id, Value = "plain" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new RichTextFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Contains));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new RichTextFieldValue { Value = "x" }), Is.EqualTo("x"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
