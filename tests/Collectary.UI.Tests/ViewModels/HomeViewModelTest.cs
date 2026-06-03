using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.UI.Services;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class HomeViewModelTest
{
    private IPresetUseCase _presetUseCase = null!;
    private IItemUseCase _itemUseCase = null!;
    private IDialogService _dialogService = null!;
    private HomeViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _presetUseCase = A.Fake<IPresetUseCase>();
        _itemUseCase = A.Fake<IItemUseCase>();
        _dialogService = A.Fake<IDialogService>();
        _sut = new HomeViewModel(_presetUseCase, _itemUseCase, _dialogService);
    }

    [Test]
    public async Task LoadAsync_PopulatesRowsFromUseCase()
    {
        var presetA = new Preset { Name = "A" };
        var presetB = new Preset { Name = "B" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { presetA, presetB });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());

        await _sut.LoadAsync();

        Assert.That(_sut.Rows.Count, Is.EqualTo(2));
        Assert.That(_sut.Rows.Select(r => r.Preset.Name), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public async Task LoadAsync_ClearsExistingRowsBeforeRepopulating()
    {
        var presetA = new Preset { Name = "A" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { presetA });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());

        await _sut.LoadAsync();
        await _sut.LoadAsync();

        Assert.That(_sut.Rows.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task LoadAsync_SetsItemCountOnRow()
    {
        var preset = new Preset { Name = "Test" };
        var items = new List<Item> { new(), new(), new() };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { preset });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(preset.Id)).Returns(items);

        await _sut.LoadAsync();

        Assert.That(_sut.Rows[0].ItemCount, Is.EqualTo(3));
    }

    [Test]
    public async Task LoadAsync_FetchesItemsForEachPreset()
    {
        var presetA = new Preset { Name = "A" };
        var presetB = new Preset { Name = "B" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { presetA, presetB });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());

        await _sut.LoadAsync();

        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(presetA.Id)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(presetB.Id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SavePresetOrderAsync_PersistsCurrentRowOrder()
    {
        var presetA = new Preset { Name = "A" };
        var presetB = new Preset { Name = "B" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { presetA, presetB });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());
        await _sut.LoadAsync();

        await _sut.SavePresetOrderAsync();

        A.CallTo(() => _presetUseCase.UpdatePresetOrderAsync(
            A<IReadOnlyList<Preset>>.That.Matches(list =>
                list.Count == 2 && list[0].Name == "A" && list[1].Name == "B")))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task LoadAsync_WhenUseCaseThrows_ShowsErrorDialog()
    {
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Throws<InvalidOperationException>();

        await _sut.LoadAsync();

        A.CallTo(() => _dialogService.ShowMessageAsync(A<string>._, A<string>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task LoadAsync_WhenUseCaseThrows_RowsRemainsEmpty()
    {
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Throws<InvalidOperationException>();

        await _sut.LoadAsync();

        Assert.That(_sut.Rows, Is.Empty);
    }

    [Test]
    public async Task DeleteRowCallback_WhenUserConfirms_InvokesOnDeletePreset()
    {
        var preset = new Preset { Name = "P" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { preset });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());
        A.CallTo(() => _dialogService.ConfirmDeleteAsync(preset.Name)).Returns(true);

        var deleted = false;
        _sut.OnDeletePreset = _ => { deleted = true; return Task.CompletedTask; };
        await _sut.LoadAsync();
        await _sut.Rows[0].DeleteCommand.ExecuteAsync(null);

        Assert.That(deleted, Is.True);
    }

    [Test]
    public async Task DeleteRowCallback_WhenUserCancels_DoesNotInvokeOnDeletePreset()
    {
        var preset = new Preset { Name = "P" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { preset });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());
        A.CallTo(() => _dialogService.ConfirmDeleteAsync(A<string>._)).Returns(false);

        var deleted = false;
        _sut.OnDeletePreset = _ => { deleted = true; return Task.CompletedTask; };
        await _sut.LoadAsync();
        await _sut.Rows[0].DeleteCommand.ExecuteAsync(null);

        Assert.That(deleted, Is.False);
    }

    [Test]
    public async Task NavigateRowCommand_InvokesOnNavigateToPresetWithSamePreset()
    {
        var preset = new Preset { Name = "P" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { preset });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());

        Preset? navigated = null;
        _sut.OnNavigateToPreset = p => navigated = p;
        await _sut.LoadAsync();
        _sut.Rows[0].NavigateCommand.Execute(null);

        Assert.That(navigated, Is.SameAs(preset));
    }

    [Test]
    public async Task EditRowCommand_InvokesOnEditPresetWithSamePreset()
    {
        var preset = new Preset { Name = "P" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { preset });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());

        Preset? edited = null;
        _sut.OnEditPreset = p => edited = p;
        await _sut.LoadAsync();
        _sut.Rows[0].EditCommand.Execute(null);

        Assert.That(edited, Is.SameAs(preset));
    }

    [Test]
    public void CreatePresetCommand_InvokesOnCreatePreset()
    {
        var created = false;
        _sut.OnCreatePreset = () => created = true;

        _sut.CreatePresetCommand.Execute(null);

        Assert.That(created, Is.True);
    }

    [Test]
    public void NavigateToSystemFieldsCommand_InvokesCallback()
    {
        var called = false;
        _sut.OnNavigateToSystemFields = () => called = true;

        _sut.NavigateToSystemFieldsCommand.Execute(null);

        Assert.That(called, Is.True);
    }

    [Test]
    public async Task DeleteRowCallback_WhenNoHandlerSet_StillConfirmsWithoutThrowing()
    {
        var preset = new Preset { Name = "P" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { preset });
        A.CallTo(() => _itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());
        A.CallTo(() => _dialogService.ConfirmDeleteAsync(A<string>._)).Returns(true);
        await _sut.LoadAsync();

        Assert.DoesNotThrowAsync(() => _sut.Rows[0].DeleteCommand.ExecuteAsync(null));
    }
}
