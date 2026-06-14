using Collectary.Search;
using Collectary.Search.Avalonia.ViewModels;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FilterChipViewModelTest
{
    private readonly LocalizationProvider _loc = new();
    private int _changedCount;

    [SetUp]
    public void SetUp() => _changedCount = 0;

    private FilterChipViewModel ChoiceChip(params string[] suggestions) =>
        new("Status", suggestions, QueryOperatorKind.Equals, _loc, () => _changedCount++);

    private FilterChipViewModel TextChip(QueryOperatorKind textOperator) =>
        new("Author", [], textOperator, _loc, () => _changedCount++);

    [Test]
    public void ChoiceChip_WithSuggestions_IsChoiceStyle()
    {
        Assert.That(ChoiceChip("open", "done").IsChoiceStyle, Is.True);
        Assert.That(TextChip(QueryOperatorKind.Contains).IsChoiceStyle, Is.False);
    }

    [Test]
    public void ChoiceChip_NoSelection_IsInertAndShowsAll()
    {
        var chip = ChoiceChip("open", "done");

        Assert.That(chip.ToRow(), Is.Null);
        Assert.That(chip.HasSelection, Is.False);
        Assert.That(chip.DisplayText,
            Is.EqualTo("Status: " + LocalizationService.Instance["SearchAllValues"]));
    }

    [Test]
    public void ChoiceChip_OneChecked_YieldsEqualsRowAndShowsTheValue()
    {
        var chip = ChoiceChip("open", "done");
        chip.VisibleOptions.First(o => o.Value == "open").IsChecked = true;

        var row = chip.ToRow();
        Assert.That(row!.Operator, Is.EqualTo(QueryOperatorKind.Equals));
        Assert.That(row.Values, Is.EqualTo(new[] { "open" }));
        Assert.That(chip.DisplayText, Is.EqualTo("Status: open"));
        Assert.That(_changedCount, Is.EqualTo(1));
    }

    [Test]
    public void ChoiceChip_TwoChecked_YieldsInRowAndShowsTheCount()
    {
        var chip = ChoiceChip("open", "done", "lost");
        chip.VisibleOptions.First(o => o.Value == "open").IsChecked = true;
        chip.VisibleOptions.First(o => o.Value == "done").IsChecked = true;

        var row = chip.ToRow();
        Assert.That(row!.Operator, Is.EqualTo(QueryOperatorKind.In));
        Assert.That(row.Values, Is.EqualTo(new[] { "open", "done" }));
        Assert.That(chip.DisplayText, Is.EqualTo(
            "Status: " + string.Format(LocalizationService.Instance["SearchSelectedCount"], 2)));
        Assert.That(_changedCount, Is.EqualTo(2));
    }

    [Test]
    public void ChoiceChip_SelectionChange_RaisesDisplayText()
    {
        var chip = ChoiceChip("open");
        var raised = new List<string?>();
        chip.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        chip.VisibleOptions[0].IsChecked = true;

        Assert.That(raised, Does.Contain(nameof(FilterChipViewModel.DisplayText)));
        Assert.That(raised, Does.Contain(nameof(FilterChipViewModel.HasSelection)));
    }

    [Test]
    public void ValueSearch_FiltersOptionsButKeepsCheckedOnes()
    {
        var chip = ChoiceChip("open", "done", "on hold");
        chip.VisibleOptions.First(o => o.Value == "done").IsChecked = true;

        chip.ValueSearchText = "hold";

        Assert.That(chip.VisibleOptions.Select(o => o.Value),
            Is.EquivalentTo(new[] { "on hold", "done" }),
            "checked options stay visible even when they do not match");

        chip.ValueSearchText = "open";
        Assert.That(chip.VisibleOptions.Select(o => o.Value),
            Is.EquivalentTo(new[] { "open", "done" }));
    }

    [Test]
    public void ValueSearch_MatchesCaseInsensitively()
    {
        var chip = ChoiceChip("Open", "Done");

        chip.ValueSearchText = "OPEN";

        Assert.That(chip.VisibleOptions.Select(o => o.Value), Is.EqualTo(new[] { "Open" }));
        Assert.That(_changedCount, Is.Zero);
    }

    [Test]
    public void TextChip_FreeText_YieldsRowWithItsTextOperator()
    {
        var contains = TextChip(QueryOperatorKind.Contains);
        contains.FreeText = "twain";
        Assert.That(contains.ToRow()!.Operator, Is.EqualTo(QueryOperatorKind.Contains));
        Assert.That(contains.ToRow()!.Values, Is.EqualTo(new[] { "twain" }));
        Assert.That(contains.DisplayText, Is.EqualTo("Author: twain"));

        var equals = TextChip(QueryOperatorKind.Equals);
        equals.FreeText = "3";
        Assert.That(equals.ToRow()!.Operator, Is.EqualTo(QueryOperatorKind.Equals));
    }

    [Test]
    public void TextChip_BlankText_IsInert()
    {
        var chip = TextChip(QueryOperatorKind.Contains);
        chip.FreeText = "   ";

        Assert.That(chip.ToRow(), Is.Null);
        Assert.That(chip.HasSelection, Is.False);
        Assert.That(chip.DisplayText,
            Is.EqualTo("Author: " + LocalizationService.Instance["SearchAllValues"]));
    }

    [Test]
    public void TextChip_TextChange_NotifiesOwnerEachTime()
    {
        var chip = TextChip(QueryOperatorKind.Contains);

        chip.FreeText = "t";
        chip.FreeText = "tw";

        Assert.That(_changedCount, Is.EqualTo(2));
    }

    [Test]
    public void ApplyValues_ChecksMatchesSilentlyAndAppendsUnknownValues()
    {
        var chip = ChoiceChip("open", "done");

        chip.ApplyValues(["OPEN", "weird"]);

        Assert.That(_changedCount, Is.Zero);
        Assert.That(chip.VisibleOptions.First(o => o.Value == "open").IsChecked, Is.True);
        Assert.That(chip.VisibleOptions.First(o => o.Value == "done").IsChecked, Is.False);
        Assert.That(chip.VisibleOptions.First(o => o.Value == "weird").IsChecked, Is.True);
        Assert.That(chip.ToRow()!.Values, Is.EquivalentTo(new[] { "open", "weird" }));
    }

    [Test]
    public void ApplyValues_OnTextChip_SetsFreeTextSilently()
    {
        var chip = TextChip(QueryOperatorKind.Contains);

        chip.ApplyValues(["twain"]);

        Assert.That(_changedCount, Is.Zero);
        Assert.That(chip.FreeText, Is.EqualTo("twain"));
    }

    [Test]
    public void ClearValues_UnchecksEverythingAndNotifiesOnce()
    {
        var chip = ChoiceChip("open", "done");
        chip.VisibleOptions[0].IsChecked = true;
        chip.VisibleOptions[1].IsChecked = true;
        _changedCount = 0;

        chip.ClearValuesCommand.Execute(null);

        Assert.That(chip.ToRow(), Is.Null);
        Assert.That(_changedCount, Is.EqualTo(1));
    }

    [Test]
    public void ClearValues_OnTextChip_ClearsTheTextAndNotifiesOnce()
    {
        var chip = TextChip(QueryOperatorKind.Contains);
        chip.FreeText = "twain";
        _changedCount = 0;

        chip.ClearValuesCommand.Execute(null);

        Assert.That(chip.FreeText, Is.Empty);
        Assert.That(_changedCount, Is.EqualTo(1));
    }

    [Test]
    public void HasSelection_TracksBothChipStyles()
    {
        var choice = ChoiceChip("open", "done");
        choice.VisibleOptions.First(o => o.Value == "open").IsChecked = true;
        Assert.That(choice.HasSelection, Is.True);

        var text = TextChip(QueryOperatorKind.Contains);
        text.FreeText = "x";
        Assert.That(text.HasSelection, Is.True);
    }

    [Test]
    public void FreshTextChip_IsInertWithoutAnyAssignment()
    {
        var chip = TextChip(QueryOperatorKind.Contains);

        Assert.That(chip.ToRow(), Is.Null);
        Assert.That(chip.DisplayText,
            Is.EqualTo("Author: " + LocalizationService.Instance["SearchAllValues"]));
    }

    [Test]
    public void ApplyValues_EmptyList_OnTextChip_ClearsTheTextWithoutThrowing()
    {
        var chip = TextChip(QueryOperatorKind.Contains);
        chip.FreeText = "twain";
        _changedCount = 0;

        chip.ApplyValues([]);

        Assert.That(chip.FreeText, Is.Empty);
        Assert.That(_changedCount, Is.Zero);
    }

    [Test]
    public void ApplyValues_RaisesDisplayTextAndKeepsLaterChangesNotifying()
    {
        var chip = ChoiceChip("open", "done");
        var raised = new List<string?>();
        chip.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        chip.ApplyValues(["open"]);
        Assert.That(raised, Does.Contain(nameof(FilterChipViewModel.DisplayText)));

        chip.VisibleOptions.First(o => o.Value == "done").IsChecked = true;
        Assert.That(_changedCount, Is.EqualTo(1), "ApplyValues must not leave notifications suppressed");
    }

    [Test]
    public void OperatorHint_ReflectsTheTextOperator()
    {
        Assert.That(TextChip(QueryOperatorKind.Contains).OperatorHint,
            Is.EqualTo(LocalizationService.Instance["SearchContainsLabel"]));
        Assert.That(TextChip(QueryOperatorKind.Equals).OperatorHint,
            Is.EqualTo(LocalizationService.Instance["SearchEqualsLabel"]));
    }

    [Test]
    public void RemoveCommand_InvokesTheRemoveCallback()
    {
        var removed = 0;
        var chip = new FilterChipViewModel("Status", ["open"], QueryOperatorKind.Equals,
            _loc, () => _changedCount++, () => removed++);

        chip.RemoveCommand.Execute(null);

        Assert.That(removed, Is.EqualTo(1));
        Assert.That(_changedCount, Is.Zero);
    }

    [Test]
    public void ToRow_TrimsFreeText()
    {
        var chip = TextChip(QueryOperatorKind.Equals);
        chip.FreeText = " 3 ";

        Assert.That(chip.ToRow()!.Values, Is.EqualTo(new[] { "3" }));
    }
}
