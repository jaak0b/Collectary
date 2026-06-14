using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Search;
using Collectary.Search.ViewModels;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ItemQueryViewModelTest
{
    private IItemSearchService _searchService = null!;
    private ISearchFieldCatalog _catalog = null!;
    private SearchOutcome? _applied;
    private ItemQueryViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _searchService = A.Fake<IItemSearchService>();
        _catalog = A.Fake<ISearchFieldCatalog>();
        var status = new SingleChoiceFieldDefinition { Label = "Status" };
        status.Choices.Add(new ChoiceOption { Value = "Open" });
        A.CallTo(() => _catalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Fields = [new SearchFieldGroup("Status", [status])],
        });
        _applied = null;
        _vm = new ItemQueryViewModel(
            new ItemSearchRunner(_searchService),
            new CollectarySearchUiCatalog(_catalog),
            new QuerySuggestionEngine(new QueryLexer()),
            new LocalizationProvider(),
            onResults: result =>
            {
                _applied = result;
                return Task.CompletedTask;
            });
    }

    private List<string> _logContexts = null!;

    private ItemQueryViewModel MakeVm(params string[] fieldLabels)
    {
        var catalog = A.Fake<ISearchFieldCatalog>();
        var groups = fieldLabels.Select(l => new SearchFieldGroup(l, [new TextFieldDefinition { Label = l }])).ToArray();
        A.CallTo(() => catalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot { Fields = groups });
        _logContexts = new List<string>();
        return new ItemQueryViewModel(
            new ItemSearchRunner(_searchService),
            new CollectarySearchUiCatalog(catalog),
            new QuerySuggestionEngine(new QueryLexer()),
            new LocalizationProvider(),
            onResults: _ => Task.CompletedTask,
            logError: (_, context) => _logContexts.Add(context));
    }

    [Test]
    public async Task MoveSelection_WithSeveralSuggestions_StepsAndWrapsArithmetically()
    {
        var vm = MakeVm("Series", "Shelf", "Status");
        vm.QueryText = "S";
        vm.CaretIndex = 1;
        await vm.RefreshSuggestionsCommand.ExecuteAsync(null);
        var count = vm.Suggestions.Count;
        Assert.That(count, Is.GreaterThanOrEqualTo(2), "need several suggestions to exercise wrap arithmetic");

        Assert.That(vm.SelectedSuggestionIndex, Is.EqualTo(0));
        vm.MoveSelection(1);
        Assert.That(vm.SelectedSuggestionIndex, Is.EqualTo(1));
        vm.MoveSelection(-1);
        Assert.That(vm.SelectedSuggestionIndex, Is.EqualTo(0));
        vm.MoveSelection(-1);
        Assert.That(vm.SelectedSuggestionIndex, Is.EqualTo(count - 1), "moving up from the top wraps to the bottom");
        vm.MoveSelection(1);
        Assert.That(vm.SelectedSuggestionIndex, Is.EqualTo(0), "moving down from the bottom wraps to the top");
    }

    [Test]
    public async Task RefreshSuggestions_WithNoMatch_ClosesAndDeselects()
    {
        var vm = MakeVm("Series", "Shelf");
        vm.QueryText = "zzz";
        vm.CaretIndex = 3;

        await vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.Suggestions, Is.Empty);
            Assert.That(vm.SelectedSuggestionIndex, Is.EqualTo(-1));
            Assert.That(vm.AreSuggestionsOpen, Is.False);
        });
    }

    [Test]
    public async Task RefreshSuggestions_Twice_FetchesTheSnapshotOnceAndDoesNotAccumulate()
    {
        var catalog = A.Fake<ISearchFieldCatalog>();
        A.CallTo(() => catalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Fields = [new SearchFieldGroup("Series", [new TextFieldDefinition { Label = "Series" }])],
        });
        var vm = new ItemQueryViewModel(
            new ItemSearchRunner(_searchService), new CollectarySearchUiCatalog(catalog),
            new QuerySuggestionEngine(new QueryLexer()), new LocalizationProvider(), _ => Task.CompletedTask);

        vm.QueryText = "Se";
        vm.CaretIndex = 2;
        await vm.RefreshSuggestionsCommand.ExecuteAsync(null);
        var firstCount = vm.Suggestions.Count;
        await vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        Assert.That(vm.Suggestions.Count, Is.EqualTo(firstCount), "each refresh must clear the previous list");
        A.CallTo(() => catalog.GetSnapshotAsync()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Run_WhenServiceThrows_InvokesTheErrorLog()
    {
        var vm = MakeVm("Series");
        A.CallTo(() => _searchService.SearchAsync(A<string>._)).Throws<InvalidOperationException>();
        vm.QueryText = "Series ~ x";

        await vm.RunCommand.ExecuteAsync(null);

        Assert.That(_logContexts.Single(), Does.Contain("Series ~ x"));
    }

    [Test]
    public async Task RefreshSuggestions_WhenCatalogThrows_InvokesTheErrorLog()
    {
        var catalog = A.Fake<ISearchFieldCatalog>();
        A.CallTo(() => catalog.GetSnapshotAsync()).Throws<InvalidOperationException>();
        var contexts = new List<string>();
        var vm = new ItemQueryViewModel(
            new ItemSearchRunner(_searchService), new CollectarySearchUiCatalog(catalog),
            new QuerySuggestionEngine(new QueryLexer()), new LocalizationProvider(),
            _ => Task.CompletedTask, (_, context) => contexts.Add(context));
        vm.QueryText = "Sta";

        await vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        Assert.That(contexts.Single(), Does.Contain("Sta"));
    }

    [Test]
    public async Task AcceptSuggestion_RefreshesSuggestionsForTheNewPosition()
    {
        var vm = MakeVm("Series", "Shelf", "Status");
        vm.QueryText = "S";
        vm.CaretIndex = 1;
        await vm.RefreshSuggestionsCommand.ExecuteAsync(null);
        var series = vm.Suggestions.First(s => s.Text == "Series");

        await vm.AcceptSuggestionCommand.ExecuteAsync(series);

        Assert.That(vm.Suggestions, Is.Not.Empty,
            "accepting a field must recompute suggestions for the new caret position, not leave them empty");
        Assert.That(vm.Suggestions.Select(s => s.Text), Does.Not.Contain("Shelf"),
            "after accepting a field the stale field list must be replaced");
    }

    [Test]
    public async Task Run_Success_AppliesResultsAndClearsMessage()
    {
        var items = new[] { new Item { DisplayName = "Loco" } };
        A.CallTo(() => _searchService.SearchAsync("name ~ loco"))
            .Returns(new ItemSearchResult(items, [], []));
        _vm.QueryText = "name ~ loco";

        await _vm.RunCommand.ExecuteAsync(null);

        Assert.That(_applied!.Items, Is.EqualTo(items));
        Assert.That(_vm.QueryMessage, Is.Null);
    }

    [Test]
    public async Task Run_WithErrors_ShowsMessageAndDoesNotApply()
    {
        A.CallTo(() => _searchService.SearchAsync(A<string>._))
            .Returns(new ItemSearchResult(
                [], [new QueryError(QueryErrorCode.UnknownField, 0, 5, "Ghost")], []));
        _vm.QueryText = "Ghost = 1";

        await _vm.RunCommand.ExecuteAsync(null);

        Assert.That(_applied, Is.Null);
        Assert.That(_vm.QueryMessage, Does.Contain("Ghost"));
    }

    [Test]
    public async Task Run_WhenANewerRunFinishesFirst_DiscardsTheStaleResults()
    {
        var slow = new TaskCompletionSource<ItemSearchResult>();
        var freshItems = new[] { new Item { DisplayName = "fresh" } };
        A.CallTo(() => _searchService.SearchAsync("name ~ a")).Returns(slow.Task);
        A.CallTo(() => _searchService.SearchAsync("name ~ ab"))
            .Returns(new ItemSearchResult(freshItems, [], []));

        _vm.QueryText = "name ~ a";
        var staleRun = _vm.RunCommand.ExecuteAsync(null);
        _vm.QueryText = "name ~ ab";
        await _vm.RunCommand.ExecuteAsync(null);
        slow.SetResult(new ItemSearchResult([new Item { DisplayName = "stale" }], [], []));
        await staleRun;

        Assert.That(_applied!.Items, Is.EqualTo(freshItems));
    }

    [Test]
    public async Task Run_WhenAStaleRunErrors_KeepsTheNewerMessage()
    {
        var slow = new TaskCompletionSource<ItemSearchResult>();
        A.CallTo(() => _searchService.SearchAsync("Ghost = 1")).Returns(slow.Task);
        A.CallTo(() => _searchService.SearchAsync("name ~ ok"))
            .Returns(new ItemSearchResult([], [], []));

        _vm.QueryText = "Ghost = 1";
        var staleRun = _vm.RunCommand.ExecuteAsync(null);
        _vm.QueryText = "name ~ ok";
        await _vm.RunCommand.ExecuteAsync(null);
        slow.SetResult(new ItemSearchResult(
            [], [new QueryError(QueryErrorCode.UnknownField, 0, 5, "Ghost")], []));
        await staleRun;

        Assert.That(_vm.QueryMessage, Is.Null);
    }

    [Test]
    public async Task Run_WithNotices_AppliesResultsAndShowsNotice()
    {
        A.CallTo(() => _searchService.SearchAsync(A<string>._))
            .Returns(new ItemSearchResult(
                [], [], [new QueryNotice(QueryErrorCode.OperatorNotSupported, "Status")]));
        _vm.QueryText = "Status > 1";

        await _vm.RunCommand.ExecuteAsync(null);

        Assert.That(_applied, Is.Not.Null);
        Assert.That(_vm.QueryMessage, Does.Contain("Status"));
    }

    [Test]
    public async Task Run_WhenServiceThrows_ShowsFailureMessageInsteadOfCrashing()
    {
        A.CallTo(() => _searchService.SearchAsync(A<string>._)).Throws<InvalidOperationException>();

        await _vm.RunCommand.ExecuteAsync(null);

        Assert.That(_vm.QueryMessage, Is.Not.Null.And.Not.Empty);
        Assert.That(_applied, Is.Null);
    }

    [Test]
    public async Task RefreshSuggestions_PopulatesListAndOpensPopup()
    {
        _vm.QueryText = "Sta";
        _vm.CaretIndex = 3;

        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        Assert.That(_vm.Suggestions.Select(s => s.Text), Does.Contain("Status"));
        Assert.That(_vm.AreSuggestionsOpen, Is.True);
        Assert.That(_vm.SelectedSuggestionIndex, Is.EqualTo(0));
    }

    [Test]
    public async Task AcceptSuggestion_ReplacesTheTypedSpanAndMovesTheCaret()
    {
        _vm.QueryText = "Sta";
        _vm.CaretIndex = 3;
        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);
        var suggestion = _vm.Suggestions.First(s => s.Text == "Status");

        await _vm.AcceptSuggestionCommand.ExecuteAsync(suggestion);

        Assert.That(_vm.QueryText, Does.StartWith("Status "));
        Assert.That(_vm.CaretIndex, Is.EqualTo("Status ".Length));
    }

    [Test]
    public async Task MoveSelection_WrapsAroundTheList()
    {
        _vm.QueryText = "Sta";
        _vm.CaretIndex = 3;
        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);
        var count = _vm.Suggestions.Count;

        _vm.MoveSelection(-1);
        Assert.That(_vm.SelectedSuggestionIndex, Is.EqualTo(count - 1));

        _vm.MoveSelection(1);
        Assert.That(_vm.SelectedSuggestionIndex, Is.EqualTo(0));
    }

    [Test]
    public async Task CloseSuggestions_ClosesThePopup()
    {
        _vm.QueryText = "Sta";
        _vm.CaretIndex = 3;
        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        _vm.CloseSuggestionsCommand.Execute(null);

        Assert.That(_vm.AreSuggestionsOpen, Is.False);
    }

    [Test]
    public async Task AcceptSuggestion_WithoutArgument_UsesTheSelectedSuggestion()
    {
        _vm.QueryText = "Sta";
        _vm.CaretIndex = 3;
        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        await _vm.AcceptSuggestionCommand.ExecuteAsync(null);

        Assert.That(_vm.QueryText, Is.Not.EqualTo("Sta"));
    }

    [Test]
    public async Task AcceptSuggestion_WithNothingSelected_LeavesTheTextAlone()
    {
        _vm.QueryText = "Sta";

        await _vm.AcceptSuggestionCommand.ExecuteAsync(null);

        Assert.That(_vm.QueryText, Is.EqualTo("Sta"));
    }

    [Test]
    public void MoveSelection_WithoutSuggestions_DoesNothing()
    {
        _vm.MoveSelection(1);

        Assert.That(_vm.SelectedSuggestionIndex, Is.EqualTo(-1));
        Assert.That(_vm.AreSuggestionsOpen, Is.False);
    }

    [Test]
    public async Task Run_ErrorMessages_CoverEveryErrorKind()
    {
        var cases = new (QueryErrorCode Code, string Detail)[]
        {
            (QueryErrorCode.FieldNotSearchable, "Photo"),
            (QueryErrorCode.OperatorNotSupported, "Status"),
            (QueryErrorCode.InvalidValue, "abc"),
            (QueryErrorCode.UnexpectedToken, "x"),
        };
        foreach (var (code, detail) in cases)
        {
            A.CallTo(() => _searchService.SearchAsync(A<string>._))
                .Returns(new ItemSearchResult([], [new QueryError(code, 0, 1, detail)], []));

            await _vm.RunCommand.ExecuteAsync(null);

            Assert.That(_vm.QueryMessage, Is.Not.Null.And.Not.Empty, $"a message is required for {code}");
        }
    }

    [Test]
    public async Task RefreshSuggestions_WhenCatalogThrows_ClosesQuietly()
    {
        A.CallTo(() => _catalog.GetSnapshotAsync()).Throws<InvalidOperationException>();
        _vm.QueryText = "Sta";

        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        Assert.That(_vm.AreSuggestionsOpen, Is.False);
    }

    [Test]
    public void FreshViewModel_StartsEmptyAndUnselected()
    {
        Assert.That(_vm.QueryText, Is.EqualTo(""));
        Assert.That(_vm.SelectedSuggestionIndex, Is.EqualTo(-1));
        Assert.That(_vm.SelectedSuggestion, Is.Null);
    }

    [Test]
    public async Task SelectedSuggestion_IndexOutOfRange_IsNull()
    {
        _vm.QueryText = "Sta";
        _vm.CaretIndex = 3;
        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);

        _vm.SelectedSuggestionIndex = _vm.Suggestions.Count;
        Assert.That(_vm.SelectedSuggestion, Is.Null);
        _vm.SelectedSuggestionIndex = -1;
        Assert.That(_vm.SelectedSuggestion, Is.Null);
        _vm.SelectedSuggestionIndex = 0;
        Assert.That(_vm.SelectedSuggestion, Is.SameAs(_vm.Suggestions[0]));
    }

    [Test]
    public async Task Run_ClosesTheSuggestionPopup()
    {
        A.CallTo(() => _searchService.SearchAsync(A<string>._))
            .Returns(new ItemSearchResult([], [], []));
        _vm.QueryText = "Sta";
        _vm.CaretIndex = 3;
        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);
        Assert.That(_vm.AreSuggestionsOpen, Is.True);

        await _vm.RunCommand.ExecuteAsync(null);

        Assert.That(_vm.AreSuggestionsOpen, Is.False);
    }

    [Test]
    public async Task AcceptSuggestion_InTheMiddleOfTheText_PreservesTheTail()
    {
        _vm.QueryText = "Sta = open";
        var suggestion = new QuerySuggestion("Status", "Status", 0, 3, QuerySuggestionKind.Field);

        await _vm.AcceptSuggestionCommand.ExecuteAsync(suggestion);

        Assert.That(_vm.QueryText, Is.EqualTo("Status  = open"));
        Assert.That(_vm.CaretIndex, Is.EqualTo("Status ".Length));
    }

    [Test]
    public async Task AcceptSuggestion_WithSpanBeyondTheText_ClampsInsteadOfThrowing()
    {
        _vm.QueryText = "Sta";
        var suggestion = new QuerySuggestion("Status", "Status", 1, 99, QuerySuggestionKind.Field);

        await _vm.AcceptSuggestionCommand.ExecuteAsync(suggestion);

        Assert.That(_vm.QueryText, Is.EqualTo("SStatus "));
    }

    [Test]
    public async Task Run_SyntaxErrorMessage_NamesThePosition()
    {
        A.CallTo(() => _searchService.SearchAsync(A<string>._))
            .Returns(new ItemSearchResult([], [new QueryError(QueryErrorCode.ExpectedValue, 3, 0)], []));

        await _vm.RunCommand.ExecuteAsync(null);

        Assert.That(_vm.QueryMessage, Does.Contain("4"), "positions are reported one-based");
    }

    [Test]
    public async Task MoveSelection_StepsThroughTheListAndReopensThePopup()
    {
        _vm.QueryText = "Sta";
        _vm.CaretIndex = 3;
        await _vm.RefreshSuggestionsCommand.ExecuteAsync(null);
        _vm.CloseSuggestionsCommand.Execute(null);

        _vm.MoveSelection(1);

        Assert.That(_vm.SelectedSuggestionIndex, Is.EqualTo(1 % _vm.Suggestions.Count));
        Assert.That(_vm.AreSuggestionsOpen, Is.True);
    }
}
