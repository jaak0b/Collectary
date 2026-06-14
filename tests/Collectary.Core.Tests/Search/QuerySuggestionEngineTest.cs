using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class QuerySuggestionEngineTest
{
    private QuerySuggestionEngine _engine = null!;
    private SearchCatalogSnapshot _snapshot = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new QuerySuggestionEngine(new QueryLexer());
        var status = new SingleChoiceFieldDefinition { Label = "Status" };
        status.Choices.Add(new ChoiceOption { Value = "Open", DisplayOrder = 1 });
        status.Choices.Add(new ChoiceOption { Value = "On Hold", DisplayOrder = 2 });
        var pages = new IntegerFieldDefinition { Label = "Pages" };
        var spaced = new TextFieldDefinition { Label = "My Field" };
        _snapshot = new SearchCatalogSnapshot
        {
            Fields =
            [
                new SearchFieldGroup("Status", [status]),
                new SearchFieldGroup("Pages", [pages]),
                new SearchFieldGroup("My Field", [spaced]),
            ],
            Presets =
            [
                new SearchPresetEntry(Guid.NewGuid(), "Books"),
                new SearchPresetEntry(Guid.NewGuid(), "Games"),
            ],
        };
    }

    private IReadOnlyList<QuerySuggestion> Suggest(string text) =>
        _engine.Suggest(text, text.Length, Ui(_snapshot));

    private static SearchUiSnapshot Ui(SearchCatalogSnapshot snapshot)
    {
        var catalog = A.Fake<ISearchFieldCatalog>();
        A.CallTo(() => catalog.GetSnapshotAsync()).Returns(snapshot);
        return new CollectarySearchUiCatalog(catalog).GetSnapshotAsync().GetAwaiter().GetResult();
    }

    private static IEnumerable<string> Texts(IEnumerable<QuerySuggestion> suggestions) =>
        suggestions.Select(s => s.Text);

    [Test]
    public void Suggest_AtStart_OffersFieldsAndPseudoFields()
    {
        var suggestions = Suggest("");

        Assert.That(Texts(suggestions), Does.Contain("Status"));
        Assert.That(Texts(suggestions), Does.Contain("name"));
        Assert.That(Texts(suggestions), Does.Contain("preset"));
        Assert.That(Texts(suggestions), Does.Contain("NOT"));
    }

    [Test]
    public void Suggest_FieldPrefix_FiltersToMatches()
    {
        var suggestions = Suggest("Sta");

        Assert.That(Texts(suggestions), Does.Contain("Status"));
        Assert.That(Texts(suggestions), Does.Not.Contain("Pages"));
        Assert.That(suggestions[0].ReplaceStart, Is.EqualTo(0));
        Assert.That(suggestions[0].ReplaceLength, Is.EqualTo(3));
    }

    [Test]
    public void Suggest_FieldLabeledLikeAReservedWord_InsertsQuoted()
    {
        var reserved = new TextFieldDefinition { Label = "Order" };
        _snapshot = new SearchCatalogSnapshot { Fields = [new SearchFieldGroup("Order", [reserved])] };

        var suggestion = Suggest("Ord").Single(s => s.Text == "Order");

        Assert.That(suggestion.InsertText, Is.EqualTo("\"Order\""));
    }

    [Test]
    public void Suggest_ValueContainingABackslash_InsertsQuotedAndEscaped()
    {
        var status = new SingleChoiceFieldDefinition { Label = "Path" };
        status.Choices.Add(new ChoiceOption { Value = @"a\b", DisplayOrder = 1 });
        _snapshot = new SearchCatalogSnapshot { Fields = [new SearchFieldGroup("Path", [status])] };

        var suggestion = Suggest("Path = ").Single(s => s.Text == @"a\b");

        Assert.That(suggestion.InsertText, Is.EqualTo("\"a\\\\b\""));
    }

    [Test]
    public void Suggest_PresetNamedLikeAReservedWord_InsertsQuoted()
    {
        _snapshot = new SearchCatalogSnapshot
        {
            Presets = [new SearchPresetEntry(Guid.NewGuid(), "Empty")],
        };

        var suggestion = Suggest("preset = ").Single(s => s.Text == "Empty");

        Assert.That(suggestion.InsertText, Is.EqualTo("\"Empty\""));
    }

    [Test]
    public void Suggest_FieldWithSpaces_InsertsQuoted()
    {
        var spaced = Suggest("My").Single(s => s.Text == "My Field");

        Assert.That(spaced.InsertText, Is.EqualTo("\"My Field\""));
    }

    [Test]
    public void Suggest_AfterChoiceField_OffersItsOperatorsOnly()
    {
        var texts = Texts(Suggest("Status ")).ToList();

        Assert.That(texts, Does.Contain("="));
        Assert.That(texts, Does.Contain("~"));
        Assert.That(texts, Does.Contain("in"));
        Assert.That(texts, Does.Contain("is empty"));
        Assert.That(texts, Does.Not.Contain("<"));
    }

    [Test]
    public void Suggest_AfterNumericField_OffersRelationalOperators()
    {
        var texts = Texts(Suggest("Pages ")).ToList();

        Assert.That(texts, Does.Contain("<"));
        Assert.That(texts, Does.Contain(">="));
    }

    [Test]
    public void Suggest_AfterOperator_OffersChoiceValues()
    {
        var suggestions = Suggest("Status = ");

        Assert.That(Texts(suggestions), Does.Contain("Open"));
        var spacedValue = suggestions.Single(s => s.Text == "On Hold");
        Assert.That(spacedValue.InsertText, Is.EqualTo("\"On Hold\""));
    }

    [Test]
    public void Suggest_ValuePrefix_FiltersValues()
    {
        var suggestions = Suggest("Status = Op");

        Assert.That(Texts(suggestions), Does.Contain("Open"));
        Assert.That(Texts(suggestions), Does.Not.Contain("On Hold"));
        Assert.That(suggestions[0].ReplaceStart, Is.EqualTo("Status = ".Length));
        Assert.That(suggestions[0].ReplaceLength, Is.EqualTo(2));
    }

    [Test]
    public void Suggest_PresetValue_OffersCollectionNames()
    {
        var texts = Texts(Suggest("preset = ")).ToList();

        Assert.That(texts, Does.Contain("Books"));
        Assert.That(texts, Does.Contain("Games"));
    }

    [Test]
    public void Suggest_AfterCompleteCondition_OffersConnectives()
    {
        var texts = Texts(Suggest("Status = Open ")).ToList();

        Assert.That(texts, Does.Contain("AND"));
        Assert.That(texts, Does.Contain("OR"));
        Assert.That(texts, Does.Contain("ORDER BY"));
    }

    [Test]
    public void Suggest_InsideInList_OffersValuesAfterParenAndComma()
    {
        Assert.That(Texts(Suggest("Status in (")), Does.Contain("Open"));
        Assert.That(Texts(Suggest("Status in (Open, ")), Does.Contain("On Hold"));
    }

    [Test]
    public void Suggest_AfterIs_OffersEmptinessContinuations()
    {
        var texts = Texts(Suggest("Status is ")).ToList();

        Assert.That(texts, Does.Contain("empty"));
        Assert.That(texts, Does.Contain("not empty"));
    }

    [Test]
    public void Suggest_AfterOrderBy_OffersFields()
    {
        var texts = Texts(Suggest("Status = Open ORDER BY ")).ToList();

        Assert.That(texts, Does.Contain("Status"));
        Assert.That(texts, Does.Contain("name"));
        Assert.That(texts, Does.Not.Contain("AND"));
    }

    [Test]
    public void Suggest_AfterOrderField_OffersDirections()
    {
        var texts = Texts(Suggest("ORDER BY Status ")).ToList();

        Assert.That(texts, Does.Contain("DESC"));
        Assert.That(texts, Does.Contain("ASC"));
    }

    [Test]
    public void Suggest_AfterAnd_OffersFieldsAgain()
    {
        var texts = Texts(Suggest("Status = Open AND ")).ToList();

        Assert.That(texts, Does.Contain("Pages"));
        Assert.That(texts, Does.Contain("NOT"));
    }

    [Test]
    public void Suggest_InsideUnterminatedQuotedValue_StillSuggestsValues()
    {
        var suggestions = Suggest("Status = \"On");

        var spacedValue = suggestions.Single(s => s.Text == "On Hold");
        Assert.That(spacedValue.ReplaceStart, Is.EqualTo("Status = ".Length));
        Assert.That(spacedValue.ReplaceLength, Is.EqualTo(3));
    }

    [Test]
    public void Suggest_AfterNot_OffersInContinuation()
    {
        Assert.That(Texts(Suggest("Status not ")), Does.Contain("in"));
    }

    [Test]
    public void Suggest_AfterIsNot_OffersEmpty()
    {
        Assert.That(Texts(Suggest("Status is not ")), Is.EqualTo(new[] { "empty" }));
    }

    [Test]
    public void Suggest_AfterIn_OffersTheOpeningParenthesis()
    {
        Assert.That(Texts(Suggest("Status in ")), Does.Contain("("));
    }

    [Test]
    public void Suggest_AfterOrderDirection_OffersNothing()
    {
        Assert.That(Suggest("ORDER BY Status DESC "), Is.Empty);
    }

    [Test]
    public void Suggest_InsideParenthesizedGroup_OffersFields()
    {
        Assert.That(Texts(Suggest("( ")), Does.Contain("Status"));
    }

    [Test]
    public void Suggest_OperatorsForPseudoFields_ComeFromTheirCatalog()
    {
        Assert.That(Texts(Suggest("preset ")), Does.Contain("~"));
        Assert.That(Texts(Suggest("preset ")), Does.Not.Contain("<"));
        Assert.That(Texts(Suggest("created ")), Does.Contain("<"));
    }

    [Test]
    public void Suggest_AfterCompletedEmptinessCheck_OffersConnectives()
    {
        Assert.That(Texts(Suggest("Status is empty ")), Does.Contain("AND"));
        Assert.That(Texts(Suggest("Status is not empty ")), Does.Contain("OR"));
    }

    [Test]
    public void Suggest_AfterOrderByComma_OffersFieldsAgain()
    {
        Assert.That(Texts(Suggest("ORDER BY Status DESC, ")), Does.Contain("Pages"));
    }

    [Test]
    public void Suggest_AfterClosedGroup_OffersConnectives()
    {
        Assert.That(Texts(Suggest("( Status = Open ) ")), Does.Contain("AND"));
    }

    [Test]
    public void Suggest_NumericFieldOperators_IncludeEmptinessChecks()
    {
        Assert.That(Texts(Suggest("Pages ")), Does.Contain("is not empty"));
        Assert.That(Texts(Suggest("Pages ")), Does.Contain("!="));
    }

    [Test]
    public void Suggest_OperatorsForUnknownField_AreEmpty()
    {
        Assert.That(Suggest("Ghost "), Is.Empty);
    }

    [Test]
    public void Suggest_RanksPrefixMatchesBeforeContainsMatches()
    {
        var status = new SingleChoiceFieldDefinition { Label = "State" };
        var snapshot = new SearchCatalogSnapshot
        {
            Fields =
            [
                new SearchFieldGroup("State", [status]),
                new SearchFieldGroup("Real Estate", [new TextFieldDefinition { Label = "Real Estate" }]),
            ],
        };

        var texts = _engine.Suggest("Sta", 3, Ui(snapshot)).Select(s => s.Text).ToList();

        Assert.That(texts.IndexOf("State"), Is.LessThan(texts.IndexOf("Real Estate")));
    }
}
