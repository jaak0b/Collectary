using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelCloudTest
{
    private string _dir = null!;
    private string _original = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_dir);
        _original = AppPreferences.FilePath;
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _original;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static SettingsViewModel Make(
        Func<CloudProvider, Task<string?>>? connect = null,
        Func<CloudProvider, Task<CloudFolder?>>? pickCloudFolder = null,
        Func<CloudProvider, Task>? disconnect = null,
        Func<string?>? detect = null,
        Action? onSyncChanged = null) =>
        new(() => { }, pickFolder: null, onSyncChanged: onSyncChanged,
            connectCloud: connect, pickCloudFolder: pickCloudFolder,
            disconnectCloud: disconnect, detectInstalledCloudFolder: detect);

    [Test]
    public void SyncProvider_Switch_PersistsAndRaisesChanged()
    {
        var raised = 0;
        var sut = Make(onSyncChanged: () => raised++);

        sut.SyncProvider = CloudProvider.OneDrive;

        Assert.Multiple(() =>
        {
            Assert.That(AppPreferences.Load().SyncProvider, Is.EqualTo(CloudProvider.OneDrive));
            Assert.That(raised, Is.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public async Task Connect_Success_StoresAccountAndMarksConnected()
    {
        var sut = Make(connect: _ => Task.FromResult<string?>("me@example.com"));
        sut.SyncProvider = CloudProvider.OneDrive;

        await sut.ConnectCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsCloudConnected, Is.True);
            Assert.That(sut.AccountLabel, Is.EqualTo("me@example.com"));
            Assert.That(AppPreferences.Load().OneDriveAccount, Is.EqualTo("me@example.com"));
        });
    }

    [Test]
    public async Task Connect_Cancelled_DoesNotMarkConnected()
    {
        var sut = Make(connect: _ => Task.FromResult<string?>(null));
        sut.SyncProvider = CloudProvider.OneDrive;

        await sut.ConnectCommand.ExecuteAsync(null);

        Assert.That(sut.IsCloudConnected, Is.False);
    }

    [Test]
    public async Task ChooseCloudFolder_Selected_PersistsRootId()
    {
        var raised = 0;
        var sut = Make(
            pickCloudFolder: _ => Task.FromResult<CloudFolder?>(new CloudFolder("folder-99", "Collectary")),
            onSyncChanged: () => raised++);
        sut.SyncProvider = CloudProvider.OneDrive;
        raised = 0;

        await sut.ChooseCloudFolderCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(AppPreferences.Load().OneDriveRootFolderId, Is.EqualTo("folder-99"));
            Assert.That(AppPreferences.Load().OneDriveRootFolderName, Is.EqualTo("Collectary"));
            Assert.That(sut.SelectedFolderName, Is.EqualTo("Collectary"));
            Assert.That(sut.IsSyncConfigured, Is.True);
            Assert.That(raised, Is.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public async Task ChooseCloudFolder_Cancelled_LeavesRootUnset()
    {
        var sut = Make(pickCloudFolder: _ => Task.FromResult<CloudFolder?>(null));
        sut.SyncProvider = CloudProvider.OneDrive;

        await sut.ChooseCloudFolderCommand.ExecuteAsync(null);

        Assert.That(AppPreferences.Load().OneDriveRootFolderId, Is.Null);
    }

    [Test]
    public async Task Disconnect_ClearsAccountAndFolder_AndSignsOut()
    {
        var signedOut = false;
        var sut = Make(
            connect: _ => Task.FromResult<string?>("me@example.com"),
            pickCloudFolder: _ => Task.FromResult<CloudFolder?>(new CloudFolder("f1", "F")),
            disconnect: _ => { signedOut = true; return Task.CompletedTask; });
        sut.SyncProvider = CloudProvider.OneDrive;
        await sut.ConnectCommand.ExecuteAsync(null);
        await sut.ChooseCloudFolderCommand.ExecuteAsync(null);

        await sut.DisconnectCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(signedOut, Is.True);
            Assert.That(sut.IsCloudConnected, Is.False);
            Assert.That(AppPreferences.Load().OneDriveAccount, Is.Null);
            Assert.That(AppPreferences.Load().OneDriveRootFolderId, Is.Null);
            Assert.That(AppPreferences.Load().OneDriveRootFolderName, Is.Null);
        });
    }

    [Test]
    public void AutoDetectLocalCloudFolder_Found_SetsFolderProviderAndLocation()
    {
        var sut = Make(detect: () => @"C:\Users\me\OneDrive");
        sut.SyncProvider = CloudProvider.OneDrive;

        sut.AutoDetectLocalCloudFolderCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.SyncProvider, Is.EqualTo(CloudProvider.Folder));
            Assert.That(sut.SyncLocation, Is.EqualTo(@"C:\Users\me\OneDrive"));
        });
    }

    [Test]
    public async Task ConnectionStatus_WhenConnected_IncludesAccount()
    {
        var sut = Make(connect: _ => Task.FromResult<string?>("me@example.com"));
        sut.SyncProvider = CloudProvider.OneDrive;

        await sut.ConnectCommand.ExecuteAsync(null);

        Assert.That(sut.ConnectionStatus, Does.Contain("me@example.com"));
    }

    [Test]
    public void Constructor_LoadsCloudProviderAndAccount()
    {
        AppPreferences.Save(new AppPreferencesData(
            SyncProvider: CloudProvider.OneDrive,
            OneDriveAccount: "stored@example.com",
            OneDriveRootFolderId: "stored-folder"));

        var sut = Make();

        Assert.Multiple(() =>
        {
            Assert.That(sut.SyncProvider, Is.EqualTo(CloudProvider.OneDrive));
            Assert.That(sut.AccountLabel, Is.EqualTo("stored@example.com"));
            Assert.That(sut.IsCloudConnected, Is.True);
        });
    }
}
