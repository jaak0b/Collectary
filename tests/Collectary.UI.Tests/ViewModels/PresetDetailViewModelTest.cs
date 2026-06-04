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
    private IListCellBuilder _listCellBuilder = null!;
    private IDialogService _dialogService = null!;

    [SetUp]
    public void SetUp()
    {
        _itemUseCase = A.Fake<IItemUseCase>();
        _presetUseCase = A.Fake<IPresetUseCase>();
        _listCellBuilder = A.Fake<IListCellBuilder>();
        _dialogService = A.Fake<IDialogService>();

        A.CallTo(() => _presetUseCase.GetEffectiveFieldsAsync(A<Guid>._)).Returns(new EffectiveFields());
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());
        A.CallTo(() => _listCellBuilder.Build(A<IReadOnlyList<FieldValue>>._, A<IReadOnlyList<FieldDefinition>>._))
            .Returns((IReadOnlyList<ListCellViewModelBase>)new List<ListCellViewModelBase>());
    }

    private PresetDetailViewModel CreateSut(
        Preset? preset = null,
        Action<Preset, EffectiveFields, Item?>? navigateToEditor = null,
        Action? navigateBack = null) =>
        new(
            preset ?? new Preset { Name = "Test" },
            _itemUseCase,
            _presetUseCase,
            _listCellBuilder,
            _dialogService,
            navigateToItemEditor: navigateToEditor ?? ((_, _, _) => { }),
            navigateBack: navigateBack ?? (() => { }));

    [Test]
    public async Task LoadAsync_PopulatesItemRowsFromUseCase()
    {
        var preset = new Preset();
        var items = new List<Item> { new() { DisplayName = "A" }, new() { DisplayName = "B" } };
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(preset.Id)).Returns(items);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();

        Assert.That(sut.ItemRows.Count, Is.EqualTo(2));
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
        var existingItem = new Item { DisplayName = "Existing" };
        var items = new List<Item> { existingItem };
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(preset.Id)).Returns(items);

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
        var item = new Item { DisplayName = "Delete Me" };
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(preset.Id)).Returns(new List<Item> { item });
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
        var item = new Item { DisplayName = "Keep Me" };
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(preset.Id)).Returns(new List<Item> { item });
        A.CallTo(() => _dialogService.ConfirmDeleteAsync(A<string>._)).Returns(false);

        var sut = CreateSut(preset: preset);
        await sut.LoadAsync();
        var row = sut.ItemRows[0];

        await sut.DeleteItemCommand.ExecuteAsync(row);

        A.CallTo(() => _itemUseCase.DeleteItemAsync(A<Guid>._)).MustNotHaveHappened();
        Assert.That(sut.ItemRows, Does.Contain(row));
    }
}
