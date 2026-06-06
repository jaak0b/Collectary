using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.ViewModels;
using FakeItEasy;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ShareDialogViewModelTest
{
    private IShareUseCase _shares = null!;
    private readonly Guid _presetId = Guid.NewGuid();
    private int _transferredCalls;

    [SetUp]
    public void SetUp()
    {
        _shares = A.Fake<IShareUseCase>();
        _transferredCalls = 0;
    }

    private ShareDialogViewModel Make() =>
        new(_shares, _presetId, "Model trains", () => _transferredCalls++);

    [Test]
    public async Task LoadAsync_PopulatesShares()
    {
        A.CallTo(() => _shares.ListSharesAsync(_presetId)).Returns(new List<ShareInfo>
        {
            new(Guid.NewGuid(), "bob", "Bob", SharePermission.Edit),
        });
        var vm = Make();

        await vm.LoadAsync();

        Assert.That(vm.Shares, Has.Count.EqualTo(1));
        Assert.That(vm.HasShares, Is.True);
    }

    [Test]
    public async Task ShareCommand_Success_SharesAndClearsTarget()
    {
        var vm = Make();
        vm.TargetUsername = "bob";
        vm.SelectedPermission = SharePermission.Edit;

        await vm.ShareCommand.ExecuteAsync(null);

        A.CallTo(() => _shares.ShareAsync(_presetId, "bob", SharePermission.Edit)).MustHaveHappenedOnceExactly();
        Assert.That(vm.TargetUsername, Is.Empty);
    }

    [Test]
    public async Task ShareCommand_UserNotFound_SetsError()
    {
        A.CallTo(() => _shares.ShareAsync(_presetId, A<string>._, A<SharePermission>._))
            .Throws(new UserNotFoundException("ghost"));
        var vm = Make();
        vm.TargetUsername = "ghost";

        await vm.ShareCommand.ExecuteAsync(null);

        Assert.That(vm.HasError, Is.True);
    }

    [Test]
    public async Task ShareCommand_NotOwner_SetsError()
    {
        A.CallTo(() => _shares.ShareAsync(_presetId, A<string>._, A<SharePermission>._))
            .Throws(new UnauthorizedAccessException());
        var vm = Make();
        vm.TargetUsername = "bob";

        await vm.ShareCommand.ExecuteAsync(null);

        Assert.That(vm.HasError, Is.True);
    }

    [Test]
    public async Task RevokeCommand_RevokesAndReloads()
    {
        var share = new ShareInfo(Guid.NewGuid(), "bob", "Bob", SharePermission.Read);
        var vm = Make();

        await vm.RevokeCommand.ExecuteAsync(share);

        A.CallTo(() => _shares.RevokeAsync(_presetId, "bob")).MustHaveHappenedOnceExactly();
        A.CallTo(() => _shares.ListSharesAsync(_presetId)).MustHaveHappened();
    }

    [Test]
    public async Task TransferCommand_Success_SetsStatusAndNotifies()
    {
        var vm = Make();
        vm.TransferUsername = "bob";

        await vm.TransferCommand.ExecuteAsync(null);

        A.CallTo(() => _shares.TransferAsync(_presetId, "bob")).MustHaveHappenedOnceExactly();
        Assert.That(vm.StatusMessage, Is.Not.Null.And.Not.Empty);
        Assert.That(_transferredCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task TransferCommand_UserNotFound_SetsError()
    {
        A.CallTo(() => _shares.TransferAsync(_presetId, A<string>._))
            .Throws(new UserNotFoundException("ghost"));
        var vm = Make();
        vm.TransferUsername = "ghost";

        await vm.TransferCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.HasError, Is.True);
            Assert.That(_transferredCalls, Is.EqualTo(0));
        });
    }

    [Test]
    public void CloseCommand_InvokesOnBack()
    {
        var backCalls = 0;
        var vm = new ShareDialogViewModel(_shares, _presetId, "Model trains", onBack: () => backCalls++);

        vm.CloseCommand.Execute(null);

        Assert.That(backCalls, Is.EqualTo(1));
    }
}
