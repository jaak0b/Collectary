using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.SharedFields;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SharedFieldLibraryViewModelExtraTest
{
    private ISharedFieldUseCase _useCase = null!;
    private IDialogService _dialogService = null!;
    private SharedFieldLibraryViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _useCase = A.Fake<ISharedFieldUseCase>();
        _dialogService = A.Fake<IDialogService>();
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField>());
        _sut = new SharedFieldLibraryViewModel(_useCase, _dialogService, new TestFieldEditorMapper().Create(), onDone: () => { });
    }

    private static SharedField MakeField(string name) =>
        new() { Name = name, Definition = new TextFieldDefinition { Label = name } };

    [Test]
    public void DoesNotSupportGroups()
    {
        Assert.That(_sut.CurrentLevelSupportsGroups, Is.False);
    }

    [Test]
    public void AddGroup_IsNoOp_BecauseGroupsUnsupported()
    {
        _sut.AddGroupCommand.Execute(null);

        Assert.That(_sut.CurrentRows.OfType<FieldGroupRowViewModel>(), Is.Empty);
    }

    [Test]
    public async Task LoadAsync_PopulatesRowsFromUseCase()
    {
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { MakeField("A"), MakeField("B") });

        await _sut.LoadAsync();

        Assert.That(_sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task LoadAsync_ClearsBeforeRepopulating()
    {
        A.CallTo(() => _useCase.GetAllAsync()).Returns(new List<SharedField> { MakeField("A") });

        await _sut.LoadAsync();
        await _sut.LoadAsync();

        Assert.That(_sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task LoadAsync_WhenUseCaseThrows_ShowsDialog()
    {
        A.CallTo(() => _useCase.GetAllAsync()).Throws<InvalidOperationException>();

        await _sut.LoadAsync();

        A.CallTo(() => _dialogService.ShowMessageAsync(A<string>._, A<string>._)).MustHaveHappened();
    }
}
