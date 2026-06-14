using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Search;
using Collectary.Search.Avalonia.ViewModels;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class BasicFilterViewModelTest
{
    private List<string> _runs = null!;
    private BasicFilterViewModel _vm = null!;

    [SetUp]
    public async Task SetUp()
    {
        _runs = new List<string>();
        _vm = await MakeLoadedVm();
    }

    private async Task<BasicFilterViewModel> MakeLoadedVm(int debounceMilliseconds = 0)
    {
        var status = new SingleChoiceFieldDefinition { Label = "Status" };
        status.Choices.Add(new ChoiceOption { Value = "open" });
        status.Choices.Add(new ChoiceOption { Value = "done" });
        var author = new TextFieldDefinition { Label = "Author" };
        var price = new IntegerFieldDefinition { Label = "Price" };
        var catalog = A.Fake<ISearchFieldCatalog>();
        A.CallTo(() => catalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Fields =
            [
                new SearchFieldGroup("Status", [status]),
                new SearchFieldGroup("Author", [author]),
                new SearchFieldGroup("Price", [price]),
            ],
            Presets = [new SearchPresetEntry(Guid.NewGuid(), "Trains"), new SearchPresetEntry(Guid.NewGuid(), "Books")],
        });
        var vm = new BasicFilterViewModel(
            new CollectarySearchUiCatalog(catalog),
            new LocalizationProvider(),
            text =>
            {
                _runs.Add(text);
                return Task.CompletedTask;
            },
            debounceMilliseconds,
            excludedChipFields: ["preset"]);
        await vm.LoadAsync();
        return vm;
    }

    private async Task AwaitRun()
    {
        var pending = _vm.PendingRun;
        if (pending is not null) await pending;
    }

    private FilterChipViewModel Chip(string label) => _vm.Chips.Single(c => c.Label == label);

    [Test]
    public void Load_FieldUniverse_CombinesPseudoAndCatalogLabels()
    {
        Assert.That(_vm.AddableFields, Is.EquivalentTo(
            new[] { "collection", "created", "updated", "Status", "Author", "Price" }));
        Assert.That(_vm.SortFieldOptions, Is.EquivalentTo(
            new[] { "name", "collection", "created", "updated", "Status", "Author", "Price" }));
        Assert.That(_runs, Is.Empty);
    }

    [Test]
    public void FieldSearchText_FiltersAddableFieldsCaseInsensitively()
    {
        _vm.FieldSearchText = "PR";

        Assert.That(_vm.AddableFields, Is.EqualTo(new[] { "Price" }));
    }

    [Test]
    public async Task AddChip_CreatesAnInertChipAndOpensItsFlyout()
    {
        _vm.AddChipCommand.Execute("Status");

        await AwaitRun();
        Assert.That(_runs, Is.Empty);
        Assert.That(Chip("Status").IsFlyoutOpen, Is.True);
        Assert.That(_vm.IsMoreFlyoutOpen, Is.False);
        Assert.That(_vm.AddableFields, Does.Not.Contain("Status"));
    }

    [Test]
    public async Task SearchText_RunsANameContainsQuery()
    {
        _vm.SearchText = "loco";

        await AwaitRun();
        Assert.That(_runs, Is.EqualTo(new[] { "name ~ loco" }));
    }

    [Test]
    public async Task CheckingChipValues_RunsTheCombinedQuery()
    {
        _vm.SearchText = "loco";
        await AwaitRun();
        _vm.AddChipCommand.Execute("Status");
        Chip("Status").VisibleOptions.First(o => o.Value == "open").IsChecked = true;
        await AwaitRun();
        Chip("Status").VisibleOptions.First(o => o.Value == "done").IsChecked = true;
        await AwaitRun();

        Assert.That(_runs[^1], Is.EqualTo("name ~ loco AND Status in (open, done)"));
    }

    [Test]
    public async Task TextChips_UseContainsForTextAndEqualsForNumbers()
    {
        _vm.AddChipCommand.Execute("Author");
        Chip("Author").FreeText = "twain";
        await AwaitRun();
        Assert.That(_runs[^1], Is.EqualTo("Author ~ twain"));

        _vm.AddChipCommand.Execute("Price");
        Chip("Price").FreeText = "3";
        await AwaitRun();
        Assert.That(_runs[^1], Is.EqualTo("Author ~ twain AND Price = 3"));
    }

    [Test]
    public async Task ClearingEverything_RunsAnEmptyQuery()
    {
        _vm.SearchText = "loco";
        await AwaitRun();

        _vm.SearchText = "";
        await AwaitRun();

        Assert.That(_runs[^1], Is.EqualTo(""));
    }

    [Test]
    public async Task Sort_AppendsAnOrderByClause()
    {
        _vm.SelectedSortField = "name";
        await AwaitRun();
        Assert.That(_runs[^1], Is.EqualTo("ORDER BY name"));

        _vm.SortDescending = true;
        await AwaitRun();
        Assert.That(_runs[^1], Is.EqualTo("ORDER BY name DESC"));
    }

    [Test]
    public void Debounce_CoalescesRapidChangesIntoOneRun()
    {
        // The assembly-wide headless Avalonia SynchronizationContext never pumps, so the timed
        // debounce continuation would deadlock on it; the app's real dispatcher context does pump.
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            _runs.Clear();
            var vm = MakeLoadedVm(debounceMilliseconds: 80).GetAwaiter().GetResult();

            vm.SearchText = "l";
            vm.SearchText = "lo";
            vm.SearchText = "loco";
            vm.PendingRun!.GetAwaiter().GetResult();

            Assert.That(_runs, Is.EqualTo(new[] { "name ~ loco" }));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }

    [Test]
    public void CancelPendingRun_DropsTheScheduledRun()
    {
        // The assembly-wide headless Avalonia SynchronizationContext never pumps, so the timed
        // debounce continuation would deadlock on it; the app's real dispatcher context does pump.
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            _runs.Clear();
            var vm = MakeLoadedVm(debounceMilliseconds: 80).GetAwaiter().GetResult();

            vm.SearchText = "loco";
            vm.CancelPendingRun();
            vm.PendingRun!.GetAwaiter().GetResult();

            Assert.That(_runs, Is.Empty);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }

    [Test]
    public async Task RemoveChip_RunsAgainWithoutTheChip()
    {
        _vm.AddChipCommand.Execute("Status");
        Chip("Status").VisibleOptions.First(o => o.Value == "open").IsChecked = true;
        await AwaitRun();

        _vm.RemoveChipCommand.Execute(Chip("Status"));
        await AwaitRun();

        Assert.That(_runs[^1], Is.EqualTo(""));
        Assert.That(_vm.Chips, Is.Empty);
        Assert.That(_vm.AddableFields, Does.Contain("Status"));
    }

    [Test]
    public async Task TryLoadFromText_RepresentableQuery_PopulatesTheBarWithoutRunning()
    {
        var loaded = _vm.TryLoadFromText("name ~ loco AND Status in (open, done) AND Price = 3 ORDER BY name DESC");

        Assert.That(loaded, Is.True);
        await AwaitRun();
        Assert.That(_runs, Is.Empty);
        Assert.That(_vm.SearchText, Is.EqualTo("loco"));
        Assert.That(Chip("Status").ToRow()!.Values, Is.EqualTo(new[] { "open", "done" }));
        Assert.That(Chip("Price").FreeText, Is.EqualTo("3"));
        Assert.That(_vm.SelectedSortField, Is.EqualTo("name"));
        Assert.That(_vm.SortDescending, Is.True);
    }

    [Test]
    public void TryLoadFromText_RoundTripsToTheSameText()
    {
        var text = "name ~ loco AND Status in (open, done) AND Price = 3 ORDER BY name DESC";

        Assert.That(_vm.TryLoadFromText(text), Is.True);

        Assert.That(_vm.ToQueryText(), Is.EqualTo(text));
    }

    [Test]
    public void TryLoadFromText_PresetEquals_BecomesACollectionChipWithTheNameChecked()
    {
        var loaded = _vm.TryLoadFromText("preset = \"Trains\"");

        Assert.That(loaded, Is.True);
        var chip = Chip("collection");
        Assert.That(chip.ToRow()!.Values, Is.EqualTo(new[] { "Trains" }));
        Assert.That(chip.VisibleOptions.Select(o => o.Value), Does.Contain("Books"));
    }

    [Test]
    public void TryLoadFromText_UnknownCheckedValue_IsAppendedToTheChip()
    {
        var loaded = _vm.TryLoadFromText("Status = weird");

        Assert.That(loaded, Is.True);
        Assert.That(Chip("Status").ToRow()!.Values, Is.EqualTo(new[] { "weird" }));
    }

    [TestCase("a = 1 OR b = 2")]
    [TestCase("Price > 3")]
    [TestCase("Ghost = 1")]
    [TestCase("name = loco")]
    [TestCase("Author = twain")]
    [TestCase("Status = open ORDER BY Ghost")]
    [TestCase("Status = open ORDER BY name, Price")]
    public void TryLoadFromText_QueriesBeyondTheBar_ReturnFalseAndLeaveTheBarAlone(string text)
    {
        _vm.SearchText = "before";

        Assert.That(_vm.TryLoadFromText(text), Is.False);
        Assert.That(_vm.SearchText, Is.EqualTo("before"));
    }

    [Test]
    public async Task AddChip_UnknownLabel_DoesNothing()
    {
        _vm.AddChipCommand.Execute("Ghost");

        await AwaitRun();
        Assert.That(_vm.Chips, Is.Empty);
        Assert.That(_runs, Is.Empty);
    }

    [Test]
    public void TryLoadFromText_WithoutSort_ResetsTheSortControls()
    {
        _vm.SelectedSortField = "name";
        _vm.SortDescending = true;

        Assert.That(_vm.TryLoadFromText("Status = open"), Is.True);

        Assert.That(_vm.SelectedSortField, Is.Null);
        Assert.That(_vm.SortDescending, Is.False);
    }

    [Test]
    public async Task Load_CatalogLabelsCollidingWithBuiltIns_AreNotListedTwice()
    {
        var nameField = new TextFieldDefinition { Label = "name" };
        var presetField = new TextFieldDefinition { Label = "preset" };
        var collectionField = new TextFieldDefinition { Label = "Collection" };
        var catalog = A.Fake<ISearchFieldCatalog>();
        A.CallTo(() => catalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Fields =
            [
                new SearchFieldGroup("name", [nameField]),
                new SearchFieldGroup("preset", [presetField]),
                new SearchFieldGroup("Collection", [collectionField]),
            ],
        });
        var vm = new BasicFilterViewModel(
            new CollectarySearchUiCatalog(catalog), new LocalizationProvider(), _ => Task.CompletedTask, 0,
            excludedChipFields: ["preset"]);

        await vm.LoadAsync();

        Assert.That(vm.AddableFields.Count(f => string.Equals(f, "collection", StringComparison.OrdinalIgnoreCase)),
            Is.EqualTo(1));
        Assert.That(vm.AddableFields, Does.Not.Contain("name"));
        Assert.That(vm.AddableFields, Does.Not.Contain("preset"));
    }

    [Test]
    public async Task LoadAsync_RunTwice_DoesNotDuplicateAnyOptions()
    {
        await _vm.LoadAsync();

        Assert.That(_vm.SortFieldOptions, Is.Unique);
        Assert.That(_vm.AddableFields, Is.Unique);
        Assert.That(_runs, Is.Empty);
    }

    [Test]
    public async Task TryLoadFromText_ReplacesPreviousChips()
    {
        Assert.That(_vm.TryLoadFromText("Status = open"), Is.True);
        Assert.That(_vm.TryLoadFromText("Price = 3"), Is.True);

        await AwaitRun();
        Assert.That(_vm.Chips.Select(c => c.Label), Is.EqualTo(new[] { "Price" }));
        Assert.That(_vm.SearchText, Is.Empty);
    }
}
