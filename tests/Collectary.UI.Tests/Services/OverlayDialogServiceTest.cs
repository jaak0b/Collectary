using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using FakeItEasy;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class OverlayDialogServiceTest
{
    private OverlayDialogService _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new OverlayDialogService();

    [Test]
    public async Task ShowMessageAsync_ShowsThenClearsOnOk()
    {
        var task = _sut.ShowMessageAsync("Hello", "Title");

        Assert.Multiple(() =>
        {
            Assert.That(_sut.HasActiveDialog, Is.True);
            Assert.That(_sut.ActiveDialog, Is.InstanceOf<MessageDialogViewModel>());
        });

        ((MessageDialogViewModel)_sut.ActiveDialog!).OkCommand.Execute(null);
        await task;

        Assert.Multiple(() =>
        {
            Assert.That(_sut.HasActiveDialog, Is.False);
            Assert.That(_sut.ActiveDialog, Is.Null);
        });
    }

    [Test]
    public async Task ConfirmDeleteAsync_ReturnsTrueWhenConfirmed()
    {
        var task = _sut.ConfirmDeleteAsync("Widget");
        ((ConfirmDialogViewModel)_sut.ActiveDialog!).ConfirmCommand.Execute(null);

        Assert.That(await task, Is.True);
        Assert.That(_sut.HasActiveDialog, Is.False);
    }

    [Test]
    public async Task ConfirmDeleteAsync_ReturnsFalseWhenCancelled()
    {
        var task = _sut.ConfirmDeleteAsync("Widget");
        ((ConfirmDialogViewModel)_sut.ActiveDialog!).CancelCommand.Execute(null);

        Assert.That(await task, Is.False);
    }

    [Test]
    public async Task ConfirmAsync_ReturnsTrueWhenConfirmed_WithGivenLabels()
    {
        var task = _sut.ConfirmAsync("Discard?", "Discard", "Title");
        var vm = (ConfirmDialogViewModel)_sut.ActiveDialog!;

        Assert.Multiple(() =>
        {
            Assert.That(vm.Message, Is.EqualTo("Discard?"));
            Assert.That(vm.ConfirmLabel, Is.EqualTo("Discard"));
            Assert.That(vm.Title, Is.EqualTo("Title"));
            Assert.That(vm.CancelLabel, Is.EqualTo(Collectary.Presentation.Localization.LocalizationService.Instance["Cancel"]));
        });
        vm.ConfirmCommand.Execute(null);

        Assert.That(await task, Is.True);
        Assert.That(_sut.HasActiveDialog, Is.False);
    }

    [Test]
    public async Task ConfirmAsync_ReturnsFalseWhenCancelled()
    {
        var task = _sut.ConfirmAsync("Discard?", "Discard", "Title");
        ((ConfirmDialogViewModel)_sut.ActiveDialog!).CancelCommand.Execute(null);

        Assert.That(await task, Is.False);
    }

    [Test]
    public async Task ShowCloudFolderPickerAsync_ReturnsSelectedFolder()
    {
        var store = A.Fake<ICloudFileStore>();
        A.CallTo(() => store.ListFoldersAsync(A<string>._, A<CancellationToken>._))
            .Returns(new List<CloudFolder>());
        var picker = new CloudFolderPickerViewModel(store, new CloudFolder("root", "OneDrive"));

        var task = _sut.ShowCloudFolderPickerAsync(picker);
        Assert.That(_sut.ActiveDialog, Is.SameAs(picker));
        picker.SelectCommand.Execute(null);

        var result = await task;
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo("root"));
            Assert.That(_sut.HasActiveDialog, Is.False);
        });
    }

    [Test]
    public async Task ShowCloudFolderPickerAsync_ReturnsNullWhenCancelled()
    {
        var store = A.Fake<ICloudFileStore>();
        A.CallTo(() => store.ListFoldersAsync(A<string>._, A<CancellationToken>._))
            .Returns(new List<CloudFolder>());
        var picker = new CloudFolderPickerViewModel(store, new CloudFolder("root", "OneDrive"));

        var task = _sut.ShowCloudFolderPickerAsync(picker);
        picker.CancelCommand.Execute(null);

        Assert.That(await task, Is.Null);
    }

    [Test]
    public async Task ShowSyncConflictsAsync_ShowsUntilClosed()
    {
        var sync = A.Fake<ISyncService>();
        var status = A.Fake<ISyncStatus>();
        var vm = new SyncViewModel(sync, status);

        var task = _sut.ShowSyncConflictsAsync(vm);
        Assert.That(_sut.ActiveDialog, Is.SameAs(vm));

        vm.CloseCommand.Execute(null);
        await task;

        Assert.That(_sut.HasActiveDialog, Is.False);
    }

    [Test]
    public async Task SecondDialog_StaysQueuedUntilFirstCloses()
    {
        var first = _sut.ShowMessageAsync("A");
        var firstVm = (MessageDialogViewModel)_sut.ActiveDialog!;

        var second = _sut.ShowMessageAsync("B");
        Assert.That(_sut.ActiveDialog, Is.SameAs(firstVm), "second dialog must wait");

        firstVm.OkCommand.Execute(null);
        await first;

        var secondVm = (MessageDialogViewModel)_sut.ActiveDialog!;
        Assert.That(secondVm.Message, Is.EqualTo("B"));

        secondVm.OkCommand.Execute(null);
        await second;
        Assert.That(_sut.HasActiveDialog, Is.False);
    }
}
