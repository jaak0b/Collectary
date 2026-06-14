using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class PresetDetailViewModelTest
{
    private IItemUseCase _itemUseCase = null!;
    private IPresetUseCase _presetUseCase = null!;
    private IItemSearchService _searchService = null!;
    private ISearchFieldCatalog _searchCatalog = null!;
    private IListCellBuilder _listCellBuilder = null!;
    private IDialogService _dialogService = null!;

    private string _originalPreferencesPath = null!;
    private string _preferencesDir = null!;

    [SetUp]
    public void SetUp()
    {
        _originalPreferencesPath = AppPreferences.FilePath;
        _preferencesDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_preferencesDir);
        AppPreferences.FilePath = Path.Combine(_preferencesDir, "preferences.json");

        _itemUseCase = A.Fake<IItemUseCase>();
        _presetUseCase = A.Fake<IPresetUseCase>();
        _searchService = A.Fake<IItemSearchService>();
        _searchCatalog = A.Fake<ISearchFieldCatalog>();
        _listCellBuilder = A.Fake<IListCellBuilder>();
        _dialogService = A.Fake<IDialogService>();

        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(A<Guid>._)).Returns(new EffectiveFields());
        SetSearchResults();
        A.CallTo(() => _searchCatalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot());
        A.CallTo(() => _listCellBuilder.Build(A<IReadOnlyList<FieldValue>>._, A<IReadOnlyList<FieldDefinition>>._))
            .Returns((IReadOnlyList<ListCellViewModelBase>)new List<ListCellViewModelBase>());
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _originalPreferencesPath;
        Directory.Delete(_preferencesDir, true);
    }

    private void SetSearchResults(params Item[] items) =>
        A.CallTo(() => _searchService.SearchAsync(A<string>._))
            .Returns(new ItemSearchResult(items, [], []));

    private PresetDetailViewModel CreateSut(
        Preset? preset = null,
        Action<Preset, EffectiveFields, Item?>? navigateToEditor = null,
        Action? navigateBack = null) =>
        new(
            preset ?? new Preset { Name = "Test" },
            _itemUseCase,
            _presetUseCase,
            _searchService,
            _searchCatalog,
            _listCellBuilder,
            _dialogService,
            navigateToItemEditor: navigateToEditor ?? ((_, _, _) => { }),
            navigateBack: navigateBack ?? (() => { }));

    [Test]
    public async Task LoadAsync_PopulatesItemRowsFromTheSearchService()
    {
        var preset = new Preset();
        SetSearchResults(
            new Item { PresetId = preset.Id, DisplayName = "A" },
            new Item { PresetId = preset.Id, DisplayName = "B" });

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ItemRows.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task LoadAsync_PrefillsTheQueryWithAQuotedPresetClause()
    {
        var sut = CreateSut(preset: new Preset { Name = "My \"Books\"" });
        await sut.LoadAsync();

        Assert.That(sut.SearchBar.Query.QueryText, Is.EqualTo("preset = \"My \\\"Books\\\"\""));
        A.CallTo(() => _searchService.SearchAsync(sut.SearchBar.Query.QueryText)).MustHaveHappened();
    }

    [Test]
    public async Task LoadAsync_ResultsFromAnotherPreset_ShowCollectionColumn()
    {
        var preset = new Preset { Name = "Books" };
        var foreign = new SearchPresetEntry(Guid.NewGuid(), "Games");
        A.CallTo(() => _searchCatalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Presets = [new SearchPresetEntry(preset.Id, preset.Name), foreign],
        });
        SetSearchResults(
            new Item { PresetId = preset.Id, DisplayName = "Mine" },
            new Item { PresetId = foreign.Id, DisplayName = "Theirs" });

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ShowCollectionColumn, Is.True);
        Assert.That(sut.ItemRows.Select(r => r.CollectionName), Is.EqualTo(new[] { "Books", "Games" }));
    }

    [Test]
    public async Task LoadAsync_ResultsOnlyFromOwnPreset_HideCollectionColumn()
    {
        var preset = new Preset { Name = "Books" };
        SetSearchResults(new Item { PresetId = preset.Id, DisplayName = "Mine" });

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ShowCollectionColumn, Is.False);
    }

    [Test]
    public async Task EditItemAsync_ForeignItem_NavigatesWithItsOwningPreset()
    {
        var preset = new Preset { Name = "Books" };
        var foreignPreset = new Preset { Name = "Games" };
        var foreignItem = new Item { PresetId = foreignPreset.Id, DisplayName = "Catan" };
        A.CallTo(() => _presetUseCase.GetPresetAsync(foreignPreset.Id)).Returns(foreignPreset);
        SetSearchResults(foreignItem);

        Preset? capturedPreset = null;
        var sut = CreateSut(preset: preset, navigateToEditor: (p, _, _) => capturedPreset = p);
        await sut.LoadAsync();

        await sut.EditItemCommand.ExecuteAsync(sut.ItemRows[0]);

        Assert.That(capturedPreset, Is.SameAs(foreignPreset));
    }

    [Test]
    public async Task EditItemAsync_OwnItem_DoesNotRefetchThePreset()
    {
        var preset = new Preset { Name = "Books" };
        SetSearchResults(new Item { PresetId = preset.Id, DisplayName = "Mine" });

        Preset? capturedPreset = null;
        var sut = CreateSut(preset: preset, navigateToEditor: (p, _, _) => capturedPreset = p);
        await sut.LoadAsync();

        await sut.EditItemCommand.ExecuteAsync(sut.ItemRows[0]);

        Assert.That(capturedPreset, Is.SameAs(preset));
        A.CallTo(() => _presetUseCase.GetPresetAsync(A<Guid>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task EditItemAsync_ForeignItemWhosePresetIsGone_DoesNotNavigate()
    {
        var preset = new Preset { Name = "Books" };
        SetSearchResults(new Item { PresetId = Guid.NewGuid(), DisplayName = "Orphan" });
        A.CallTo(() => _presetUseCase.GetPresetAsync(A<Guid>._)).Returns((Preset?)null);

        var navigated = false;
        var sut = CreateSut(preset: preset, navigateToEditor: (_, _, _) => navigated = true);
        await sut.LoadAsync();

        await sut.EditItemCommand.ExecuteAsync(sut.ItemRows[0]);

        Assert.That(navigated, Is.False);
    }

    [Test]
    public async Task LoadAsync_ResultsOnlyFromAForeignPreset_ShowCollectionColumn()
    {
        var preset = new Preset { Name = "Books" };
        var foreign = new SearchPresetEntry(Guid.NewGuid(), "Games");
        A.CallTo(() => _searchCatalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Presets = [new SearchPresetEntry(preset.Id, preset.Name), foreign],
        });
        SetSearchResults(new Item { PresetId = foreign.Id, DisplayName = "Theirs" });

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ShowCollectionColumn, Is.True);
        Assert.That(sut.ItemRows.Single().CollectionName, Is.EqualTo("Games"));
    }

    [Test]
    public async Task LoadAsync_PresetNameWithBackslash_EscapesItInTheQuery()
    {
        var sut = CreateSut(preset: new Preset { Name = @"My\Stuff" });
        await sut.LoadAsync();

        Assert.That(sut.SearchBar.Query.QueryText, Is.EqualTo("preset = \"My\\\\Stuff\""));
    }

    [Test]
    public async Task LoadAsync_RaisesListColumnsChange()
    {
        var sut = CreateSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await sut.LoadAsync();

        Assert.That(raised, Does.Contain(nameof(PresetDetailViewModel.ListColumns)));
    }

    [Test]
    public async Task LoadAsync_ClearsErrorMessage()
    {
        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(sut.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task LoadAsync_WhenThrows_SetsErrorMessage()
    {
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(A<Guid>._)).Throws<InvalidOperationException>();

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.That(sut.ErrorMessage, Is.Not.Null);
    }

    [Test]
    public async Task LoadAsync_FiltersListFieldsByShowInListAndHasCell()
    {
        var preset = new Preset();
        var textField = new TextFieldDefinition { Label = "T", ShowInList = true };
        var hiddenField = new TextFieldDefinition { Label = "H", ShowInList = false };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { textField, hiddenField } });
        A.CallTo(() => _listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ListColumns.Count, Is.EqualTo(1));
        Assert.That(sut.ListColumns[0].Field.Label, Is.EqualTo("T"));
    }

    [Test]
    public async Task LoadAsync_ExcludesFieldsWithNoCellViewModel()
    {
        var preset = new Preset();
        var field = new TextFieldDefinition { Label = "X", ShowInList = true };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { field } });
        A.CallTo(() => _listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(false);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ListColumns, Is.Empty);
    }

    [Test]
    public async Task LoadAsync_ExcludesColumnsForFieldsInGatedOffGroup()
    {
        var preset = new Preset();
        var group = new FieldGroup { Name = "Hidden", ShowInList = false, DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "F", ShowInList = true };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id)).Returns(new EffectiveFields
        {
            Fields = new List<FieldDefinition> { field },
            Groups = new List<FieldGroup> { group },
            GroupByFieldId = new Dictionary<Guid, Guid?> { [field.Id] = group.Id }
        });
        A.CallTo(() => _listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ListColumns, Is.Empty);
    }

    [Test]
    public async Task LoadAsync_PrefixesColumnHeaderWithGroupPath()
    {
        var preset = new Preset();
        var group = new FieldGroup { Name = "Specs", ShowInList = true, PrefixColumnHeaders = true, DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "Weight", ShowInList = true };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id)).Returns(new EffectiveFields
        {
            Fields = new List<FieldDefinition> { field },
            Groups = new List<FieldGroup> { group },
            GroupByFieldId = new Dictionary<Guid, Guid?> { [field.Id] = group.Id }
        });
        A.CallTo(() => _listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ListColumns.Single().Header, Is.EqualTo("Specs › Weight"));
    }

    [Test]
    public async Task LoadAsync_DisplayNameField_IncludedEvenWithoutCellViewModel()
    {
        var preset = new Preset();
        var displayName = new DisplayNameFieldDefinition { ShowInList = true, DisplayOrder = 0 };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { displayName } });
        A.CallTo(() => _listCellBuilder.HasListCellViewModel(A<Type>._)).Returns(false);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ListColumns.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task LoadAsync_NoPrefix_WhenGroupPrefixColumnHeadersFalse()
    {
        var preset = new Preset();
        var group = new FieldGroup { Name = "Specs", ShowInList = true, PrefixColumnHeaders = false, DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "Weight", ShowInList = true };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id)).Returns(new EffectiveFields
        {
            Fields = new List<FieldDefinition> { field },
            Groups = new List<FieldGroup> { group },
            GroupByFieldId = new Dictionary<Guid, Guid?> { [field.Id] = group.Id }
        });
        A.CallTo(() => _listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ListColumns.Single().Header, Is.EqualTo("Weight"));
    }

    [Test]
    public async Task LoadAsync_OrdersColumnsByDisplayOrder()
    {
        var preset = new Preset();
        var second = new TextFieldDefinition { Label = "Second", ShowInList = true, DisplayOrder = 1 };
        var first = new TextFieldDefinition { Label = "First", ShowInList = true, DisplayOrder = 0 };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { second, first } });
        A.CallTo(() => _listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ListColumns.Select(c => c.Field.Label), Is.EqualTo(new[] { "First", "Second" }));
    }

    [Test]
    public async Task LoadAsync_NestedGroups_PrefixIncludesFullPath()
    {
        var preset = new Preset();
        var parent = new FieldGroup { Name = "Parent", ShowInList = true, DisplayOrder = 0 };
        var child = new FieldGroup { Name = "Child", ShowInList = true, PrefixColumnHeaders = true, ParentGroupId = parent.Id, DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "Weight", ShowInList = true };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id)).Returns(new EffectiveFields
        {
            Fields = new List<FieldDefinition> { field },
            Groups = new List<FieldGroup> { parent, child },
            GroupByFieldId = new Dictionary<Guid, Guid?> { [field.Id] = child.Id }
        });
        A.CallTo(() => _listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ListColumns.Single().Header, Is.EqualTo("Parent › Child › Weight"));
    }

    [Test]
    public void BackCommand_InvokesNavigateBack()
    {
        var invoked = false;
        var sut = CreateSut(navigateBack: () => { invoked = true; });

        sut.BackCommand.Execute(null);

        Assert.That(invoked, Is.True);
    }

    [Test]
    public async Task AddItemAsync_FetchesEffectiveFieldsThenNavigates()
    {
        var preset = new Preset();
        var fields = new EffectiveFields { Fields = new List<FieldDefinition> { new TextFieldDefinition() } };
        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(preset.Id)).Returns(fields);

        Preset? capturedPreset = null;
        EffectiveFields? capturedFields = null;
        Item? capturedItem = null;

        var sut = CreateSut(
            preset: preset,
            navigateToEditor: (p, f, i) => { capturedPreset = p; capturedFields = f; capturedItem = i; });

        await sut.AddItemCommand.ExecuteAsync(null);

        Assert.That(capturedPreset, Is.SameAs(preset));
        Assert.That(capturedFields, Is.SameAs(fields));
        Assert.That(capturedItem, Is.Null);
    }

    [Test]
    public async Task EditItemAsync_PassesExistingItemToNavigator()
    {
        var preset = new Preset();
        var existingItem = new Item { PresetId = preset.Id, DisplayName = "Existing" };
        SetSearchResults(existingItem);

        Item? capturedItem = null;
        var sut = CreateSut(preset: preset, navigateToEditor: (_, _, i) => { capturedItem = i; });
        await sut.LoadAsync();
        var row = sut.ItemRows[0];

        await sut.EditItemCommand.ExecuteAsync(row);

        Assert.That(capturedItem, Is.SameAs(existingItem));
    }

    [Test]
    public async Task DeleteItemAsync_WhenConfirmed_DeletesItemAndRemovesRow()
    {
        var preset = new Preset();
        var item = new Item { PresetId = preset.Id, DisplayName = "Delete Me" };
        SetSearchResults(item);
        A.CallTo(() => _dialogService.ConfirmDeleteAsync(item.DisplayName)).Returns(true);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();
        var row = sut.ItemRows[0];

        await sut.DeleteItemCommand.ExecuteAsync(row);

        A.CallTo(() => _itemUseCase.DeleteItemAsync(item.Id)).MustHaveHappenedOnceExactly();
        Assert.That(sut.ItemRows, Does.Not.Contain(row));
    }

    [Test]
    public async Task DeleteItemAsync_WhenCancelled_DoesNotDeleteOrRemoveRow()
    {
        var preset = new Preset();
        var item = new Item { PresetId = preset.Id, DisplayName = "Keep Me" };
        SetSearchResults(item);
        A.CallTo(() => _dialogService.ConfirmDeleteAsync(A<string>._)).Returns(false);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();
        var row = sut.ItemRows[0];

        await sut.DeleteItemCommand.ExecuteAsync(row);

        A.CallTo(() => _itemUseCase.DeleteItemAsync(A<Guid>._)).MustNotHaveHappened();
        Assert.That(sut.ItemRows, Does.Contain(row));
    }

    private Preset SeedSearchableCatalog(string presetName = "Trains")
    {
        var preset = new Preset { Name = presetName };
        var status = new SingleChoiceFieldDefinition { Label = "Status" };
        status.Choices.Add(new ChoiceOption { Value = "open" });
        status.Choices.Add(new ChoiceOption { Value = "done" });
        A.CallTo(() => _searchCatalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot
        {
            Fields = [new SearchFieldGroup("Status", [status])],
            Presets = [new SearchPresetEntry(preset.Id, preset.Name)],
        });
        return preset;
    }

    [Test]
    public async Task LoadAsync_DefaultPreference_OpensInBasicModeWithACollectionChip()
    {
        var preset = SeedSearchableCatalog();

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.SearchBar.IsBasicMode, Is.True);
        var chip = sut.SearchBar.BasicFilter.Chips.Single();
        Assert.That(chip.Label, Is.EqualTo("collection"));
        Assert.That(chip.ToRow()!.Values, Is.EqualTo(new[] { "Trains" }));
        A.CallTo(() => _searchService.SearchAsync("preset = Trains")).MustHaveHappened();
    }

    [Test]
    public async Task LoadAsync_PresetNameContainingComma_StillOpensInBasicMode()
    {
        var preset = SeedSearchableCatalog(presetName: "Smith, John");

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.SearchBar.IsBasicMode, Is.True);
        var chip = sut.SearchBar.BasicFilter.Chips.Single();
        Assert.That(chip.ToRow()!.Values, Is.EqualTo(new[] { "Smith, John" }));
        A.CallTo(() => _searchService.SearchAsync("preset = \"Smith, John\"")).MustHaveHappened();
    }

    [Test]
    public async Task LoadAsync_AdvancedPreference_OpensInAdvancedMode()
    {
        AppPreferences.Save(new AppPreferencesData(SearchBasicMode: false));
        var preset = SeedSearchableCatalog();

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.SearchBar.IsBasicMode, Is.False);
        Assert.That(sut.SearchBar.Query.QueryText, Is.EqualTo("preset = Trains"));
    }

    [Test]
    public async Task LoadAsync_WhenTheBarCannotRepresentTheDefaultQuery_FallsBackToAdvanced()
    {
        var sut = CreateSut(preset: new Preset { Name = "Trains" });
        await sut.LoadAsync();

        Assert.That(sut.SearchBar.IsBasicMode, Is.False);
        A.CallTo(() => _searchService.SearchAsync("preset = Trains")).MustHaveHappened();
    }

    [Test]
    public async Task SwitchToBasic_TooComplexQuery_StaysAdvancedAndShowsAMessage()
    {
        var preset = SeedSearchableCatalog();
        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();
        sut.SearchBar.SwitchToAdvancedCommand.Execute(null);
        sut.SearchBar.Query.QueryText = "Status = open OR Status = done";

        sut.SearchBar.SwitchToBasicCommand.Execute(null);

        Assert.That(sut.SearchBar.IsBasicMode, Is.False);
        Assert.That(sut.SearchBar.Query.QueryMessage, Is.Not.Null.And.Not.Empty);
        Assert.That(AppPreferences.Load().SearchBasicMode, Is.False);
    }

    [Test]
    public async Task SwitchToBasic_FlatQuery_PopulatesTheBarAndPersistsThePreference()
    {
        AppPreferences.Save(new AppPreferencesData(SearchBasicMode: false));
        var preset = SeedSearchableCatalog();
        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();
        sut.SearchBar.Query.QueryText = "Status in (open, done) AND preset = \"Trains\"";

        sut.SearchBar.SwitchToBasicCommand.Execute(null);

        Assert.That(sut.SearchBar.IsBasicMode, Is.True);
        Assert.That(sut.SearchBar.BasicFilter.Chips.Select(c => c.Label), Is.EqualTo(new[] { "Status", "collection" }));
        Assert.That(sut.SearchBar.Query.QueryMessage, Is.Null.Or.Empty);
        Assert.That(AppPreferences.Load().SearchBasicMode, Is.True);
    }

    [Test]
    public void BasicChipChange_RunsTheSearchWithTheSerializedQuery()
    {
        // The assembly-wide headless Avalonia SynchronizationContext never pumps, so the bar's
        // timed debounce continuation would deadlock on it; the app's dispatcher context does pump.
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var preset = SeedSearchableCatalog();
            var sut = CreateSut(preset: preset);
            sut.LoadAsync().GetAwaiter().GetResult();
            sut.SearchBar.BasicFilter.AddChipCommand.Execute("Status");
            var chip = sut.SearchBar.BasicFilter.Chips.Single(c => c.Label == "Status");

            chip.VisibleOptions.First(o => o.Value == "open").IsChecked = true;
            sut.SearchBar.BasicFilter.PendingRun!.GetAwaiter().GetResult();

            Assert.That(sut.SearchBar.Query.QueryText, Is.EqualTo("collection = Trains AND Status = open"));
            A.CallTo(() => _searchService.SearchAsync("collection = Trains AND Status = open"))
                .MustHaveHappened();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }

    [Test]
    public void SwitchToAdvanced_CancelsThePendingBasicRun()
    {
        // The assembly-wide headless Avalonia SynchronizationContext never pumps, so the bar's
        // timed debounce continuation would deadlock on it; the app's dispatcher context does pump.
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var preset = SeedSearchableCatalog();
            var sut = CreateSut(preset: preset);
            sut.LoadAsync().GetAwaiter().GetResult();

            sut.SearchBar.BasicFilter.SearchText = "loc";
            sut.SearchBar.SwitchToAdvancedCommand.Execute(null);
            sut.SearchBar.Query.QueryText = "Status = open OR Status = done";
            sut.SearchBar.BasicFilter.PendingRun!.GetAwaiter().GetResult();

            Assert.That(sut.SearchBar.Query.QueryText, Is.EqualTo("Status = open OR Status = done"));
            A.CallTo(() => _searchService.SearchAsync(A<string>.That.Contains("name ~ loc")))
                .MustNotHaveHappened();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }

    [Test]
    public async Task SwitchToAdvanced_SerializesTheBarAndPersistsThePreference()
    {
        var preset = SeedSearchableCatalog();
        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        sut.SearchBar.SwitchToAdvancedCommand.Execute(null);

        Assert.That(sut.SearchBar.IsBasicMode, Is.False);
        Assert.That(sut.SearchBar.Query.QueryText, Is.EqualTo("collection = Trains"));
        Assert.That(AppPreferences.Load().SearchBasicMode, Is.False);
    }
}
