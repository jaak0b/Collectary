using Collectary.Core.Ports;
using Collectary.Presentation.ViewModels;
using FakeItEasy;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class CloudFolderPickerViewModelTest
{
    private ICloudFileStore _store = null!;
    private CloudFolder _root = null!;

    [SetUp]
    public void SetUp()
    {
        _store = A.Fake<ICloudFileStore>();
        _root = new CloudFolder("root", "OneDrive");
        A.CallTo(() => _store.ListFoldersAsync("root", A<CancellationToken>._))
            .Returns(new List<CloudFolder> { new("a", "Albums"), new("b", "Backups") });
    }

    private CloudFolderPickerViewModel Build() => new(_store, _root);

    [Test]
    public async Task Initialize_LoadsSubfoldersOfRoot()
    {
        var sut = Build();

        await sut.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(sut.CurrentFolder.Id, Is.EqualTo("root"));
            Assert.That(sut.Subfolders.Select(f => f.Name), Is.EquivalentTo(new[] { "Albums", "Backups" }));
            Assert.That(sut.CanGoUp, Is.False);
        });
    }

    [Test]
    public async Task OpenFolder_NavigatesIntoChild_AndCanGoUp()
    {
        A.CallTo(() => _store.ListFoldersAsync("a", A<CancellationToken>._))
            .Returns(new List<CloudFolder> { new("a1", "2024") });
        var sut = Build();
        await sut.InitializeAsync();

        await sut.OpenFolderCommand.ExecuteAsync(new CloudFolder("a", "Albums"));

        Assert.Multiple(() =>
        {
            Assert.That(sut.CurrentFolder.Id, Is.EqualTo("a"));
            Assert.That(sut.Subfolders.Select(f => f.Name), Is.EquivalentTo(new[] { "2024" }));
            Assert.That(sut.CanGoUp, Is.True);
        });
    }

    [Test]
    public async Task GoUp_ReturnsToParent()
    {
        A.CallTo(() => _store.ListFoldersAsync("a", A<CancellationToken>._))
            .Returns(new List<CloudFolder>());
        var sut = Build();
        await sut.InitializeAsync();
        await sut.OpenFolderCommand.ExecuteAsync(new CloudFolder("a", "Albums"));

        await sut.GoUpCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.CurrentFolder.Id, Is.EqualTo("root"));
            Assert.That(sut.CanGoUp, Is.False);
        });
    }

    [Test]
    public async Task CreateFolder_CreatesUnderCurrent_AndReloads()
    {
        A.CallTo(() => _store.EnsureFolderAsync("root", "New", A<CancellationToken>._)).Returns("new-id");
        var sut = Build();
        await sut.InitializeAsync();
        sut.NewFolderName = "New";

        await sut.CreateFolderCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            A.CallTo(() => _store.EnsureFolderAsync("root", "New", A<CancellationToken>._)).MustHaveHappened();
            Assert.That(sut.NewFolderName, Is.Empty);
        });
    }

    [Test]
    public async Task CreateFolder_BlankName_DoesNothing()
    {
        var sut = Build();
        await sut.InitializeAsync();
        sut.NewFolderName = "   ";

        await sut.CreateFolderCommand.ExecuteAsync(null);

        A.CallTo(() => _store.EnsureFolderAsync(A<string>._, A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Select_RaisesCloseWithCurrentFolder()
    {
        var sut = Build();
        await sut.InitializeAsync();
        CloudFolder? result = null;
        var closed = false;
        sut.CloseRequested = f => { result = f; closed = true; };

        sut.SelectCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(closed, Is.True);
            Assert.That(result!.Id, Is.EqualTo("root"));
        });
    }

    [Test]
    public void Cancel_RaisesCloseWithNull()
    {
        var sut = Build();
        CloudFolder? result = new CloudFolder("x", "x");
        var closed = false;
        sut.CloseRequested = f => { result = f; closed = true; };

        sut.CancelCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(closed, Is.True);
            Assert.That(result, Is.Null);
        });
    }
}
