using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels.SharedFields;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SharedFieldLibraryViewModelTest
{
    private ISharedFieldUseCase _useCase = null!;
    private IDialogService _dialogService = null!;
    private SharedFieldLibraryViewModel _sut = null!;

    private static SharedField MakeField(string label) => new()
    {
        Definition = new TextFieldDefinition { Label = label }
    };

    private static SharedField MakeTrackedField(string label)
    {
        var sf = new SharedField { Name = label, Definition = new TextFieldDefinition { Label = label } };
        sf.Definition.SharedFieldId = sf.Id;
        return sf;
    }

    [SetUp]
    public void SetUp()
    {
        _useCase = A.Fake<ISharedFieldUseCase>();
        _dialogService = A.Fake<IDialogService>();
        _sut = new SharedFieldLibraryViewModel(_useCase, _dialogService, new TestFieldEditorMapper().Create(), onDone: () => { });
    }

    private SharedFieldLibraryViewModel CreateSut(Action? onDone = null) =>
        new(_useCase, _dialogService, new TestFieldEditorMapper().Create(), onDone: onDone ?? (() => { }));

    [Test]
    public async Task LoadAsync_PopulatesRowsFromUseCase()
    {
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { MakeField("Color"), MakeField("Size") });

        await _sut.LoadAsync();

        Assert.That(_sut.CurrentRows.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task LoadAsync_ClearsExistingRowsBeforeRepopulating()
    {
        A.CallTo(() => _useCase.GetAllAsync())
            .Returns(new List<SharedField> { MakeField("A") }).Once()
            .Then.Returns(new List<SharedField> { MakeField("B"), MakeField("C") });

        await _sut.LoadAsync();
        await _sut.LoadAsync();

        Assert.That(_sut.CurrentRows.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task LoadAsync_WhenUseCaseThrows_ShowsDialog()
    {
        A.CallTo(() => _useCase.GetAllAsync()).Throws<InvalidOperationException>();

        await _sut.LoadAsync();

        A.CallTo(() => _dialogService.ShowMessageAsync(A<string>._, A<string>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void Cancel_InvokesOnDoneCallback()
    {
        var invoked = false;
        var sut = CreateSut(onDone: () => { invoked = true; });

        sut.CancelCommand.Execute(null);

        Assert.That(invoked, Is.True);
    }

    [Test]
    public async Task SaveAndGoBackAsync_InvokesOnDoneAfterSave()
    {
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField>());
        var invoked = false;
        var sut = CreateSut(onDone: () => { invoked = true; });
        await sut.LoadAsync();

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(invoked, Is.True);
    }

    [Test]
    public async Task HandleSystemBackAsync_SavesInvokesOnDoneAndReturnsTrue()
    {
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField>());
        var invoked = false;
        var sut = CreateSut(onDone: () => { invoked = true; });
        await sut.LoadAsync();

        var handled = await ((Collectary.Presentation.ViewModels.ISystemBackHandler)sut).HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(invoked, Is.True);
        });
    }

    [Test]
    public async Task SaveAndGoBackAsync_WhenNested_NavigatesUpOneLevelWithoutExiting()
    {
        var listSf = new SharedField
        {
            Name = "L",
            Definition = new ListFieldDefinition { Label = "L" }
        };
        listSf.Definition.SharedFieldId = listSf.Id;
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { listSf });
        var exited = false;
        var sut = CreateSut(onDone: () => exited = true);
        await sut.LoadAsync();
        sut.DrillIntoCommand.Execute(sut.CurrentRows[0]);

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(sut.Levels.Count, Is.EqualTo(1));
        Assert.That(exited, Is.False);
    }

    [Test]
    public async Task AddTextField_AtRoot_CreatesSharedFieldViaUseCase()
    {
        await _sut.AddFieldAsync<TextFieldDefinition>();

        A.CallTo(() => _useCase.CreateAsync(A<SharedField>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddTextField_AtRoot_AddsRowToCurrentRows()
    {
        await _sut.AddFieldAsync<TextFieldDefinition>();

        Assert.That(_sut.CurrentRows.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task AddTextField_WhenNested_DoesNotCallUseCaseCreate()
    {
        var listField = new SharedField
        {
            Name = "ListSF",
            Definition = new ListFieldDefinition { Label = "MyList" }
        };
        listField.Definition.SharedFieldId = listField.Id;
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { listField });
        await _sut.LoadAsync();
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        Fake.ClearRecordedCalls(_useCase);

        await _sut.AddFieldAsync<TextFieldDefinition>();

        A.CallTo(() => _useCase.CreateAsync(A<SharedField>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task RemoveField_AtRoot_DeletesViaUseCase()
    {
        var sf = MakeTrackedField("Tag");
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { sf });
        await _sut.LoadAsync();
        var row = _sut.CurrentRows[0];

        await _sut.RemoveFieldCommand.ExecuteAsync(row);

        A.CallTo(() => _useCase.DeleteAsync(sf.Id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RemoveField_AtRoot_RemovesRowFromCurrentRows()
    {
        var sf = MakeTrackedField("Tag");
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { sf });
        await _sut.LoadAsync();
        var row = _sut.CurrentRows[0];

        await _sut.RemoveFieldCommand.ExecuteAsync(row);

        Assert.That(_sut.CurrentRows, Does.Not.Contain(row));
    }

    [Test]
    public async Task RemoveField_ClearsSelectedField_WhenRemovingSelected()
    {
        var sf = MakeTrackedField("Tag");
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { sf });
        await _sut.LoadAsync();
        var row = _sut.CurrentRows[0];
        _sut.SelectedNode = row;

        await _sut.RemoveFieldCommand.ExecuteAsync(row);

        Assert.That(_sut.SelectedNode, Is.Null);
    }

    [Test]
    public async Task SaveAsync_UpdatesEachSharedFieldViaUseCase()
    {
        var sf1 = MakeTrackedField("Alpha");
        var sf2 = MakeTrackedField("Beta");
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { sf1, sf2 });
        await _sut.LoadAsync();

        await _sut.SaveAndGoBackCommand.ExecuteAsync(null);

        A.CallTo(() => _useCase.UpdateAsync(A<SharedField>._)).MustHaveHappened(2, Times.Exactly);
    }

    [Test]
    public async Task SaveCommand_WhenThrows_ShowsDialog()
    {
        var sf = MakeTrackedField("X");
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { sf });
        A.CallTo(() => _useCase.UpdateAsync(A<SharedField>._)).Throws<InvalidOperationException>();
        await _sut.LoadAsync();

        await _sut.SaveCommand.ExecuteAsync(null);

        A.CallTo(() => _dialogService.ShowMessageAsync(A<string>._, A<string>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ReorderAsync_CallsUseCaseReorderWithCorrectOrder()
    {
        var fieldA = MakeTrackedField("A");
        var fieldB = MakeTrackedField("B");
        var fieldC = MakeTrackedField("C");
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { fieldA, fieldB, fieldC });
        await _sut.LoadAsync();

        await _sut.ReorderAsync(0, 2);

        A.CallTo(() => _useCase.ReorderAsync(
            A<IReadOnlyList<Guid>>.That.Matches(ids =>
                ids.Count == 3 && ids[2] == fieldA.Id)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ReorderAsync_WhenNested_DoesNotCallUseCase()
    {
        var listSf = new SharedField
        {
            Name = "L",
            Definition = new ListFieldDefinition { Label = "L" }
        };
        listSf.Definition.SharedFieldId = listSf.Id;
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { listSf });
        await _sut.LoadAsync();
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        Fake.ClearRecordedCalls(_useCase);

        await _sut.ReorderAsync(0, 0);

        A.CallTo(() => _useCase.ReorderAsync(A<IReadOnlyList<Guid>>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task CommitReorderAsync_PersistsCurrentOrder()
    {
        var fieldA = MakeTrackedField("A");
        var fieldB = MakeTrackedField("B");
        var fieldC = MakeTrackedField("C");
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { fieldA, fieldB, fieldC });
        await _sut.LoadAsync();

        _sut.MoveField(0, 2);
        await _sut.CommitReorderAsync();

        A.CallTo(() => _useCase.ReorderAsync(
            A<IReadOnlyList<Guid>>.That.Matches(ids =>
                ids.Count == 3 && ids[2] == fieldA.Id)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task CommitReorderAsync_WhenNested_DoesNotCallUseCase()
    {
        var listSf = new SharedField
        {
            Name = "L",
            Definition = new ListFieldDefinition { Label = "L" }
        };
        listSf.Definition.SharedFieldId = listSf.Id;
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { listSf });
        await _sut.LoadAsync();
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        Fake.ClearRecordedCalls(_useCase);

        await _sut.CommitReorderAsync();

        A.CallTo(() => _useCase.ReorderAsync(A<IReadOnlyList<Guid>>._)).MustNotHaveHappened();
    }
}
