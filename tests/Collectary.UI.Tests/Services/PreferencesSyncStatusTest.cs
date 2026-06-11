using Collectary.Core.Domain;
using Collectary.Presentation.Services;
using Collectary.UI.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class PreferencesSyncStatusTest
{
    private string _dir = null!;
    private string _original = null!;
    private PreferencesSyncStatus _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_dir);
        _original = AppPreferences.FilePath;
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
        _sut = new PreferencesSyncStatus();
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _original;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Test]
    public void IsConfigured_FolderWithLocation_True()
    {
        AppPreferences.Save(new AppPreferencesData(SyncProvider: CloudProvider.Folder, SyncLocation: @"C:\sync"));
        Assert.That(_sut.IsConfigured, Is.True);
    }

    [Test]
    public void IsConfigured_FolderWithoutLocation_False()
    {
        AppPreferences.Save(new AppPreferencesData(SyncProvider: CloudProvider.Folder, SyncLocation: null));
        Assert.That(_sut.IsConfigured, Is.False);
    }

    [Test]
    public void IsConfigured_OneDriveWithRootFolder_True()
    {
        AppPreferences.Save(new AppPreferencesData(SyncProvider: CloudProvider.OneDrive, OneDriveRootFolderId: "folder-1"));
        Assert.That(_sut.IsConfigured, Is.True);
    }

    [Test]
    public void IsConfigured_OneDriveWithoutRootFolder_False()
    {
        AppPreferences.Save(new AppPreferencesData(SyncProvider: CloudProvider.OneDrive, OneDriveRootFolderId: null));
        Assert.That(_sut.IsConfigured, Is.False);
    }

    [Test]
    public void IsConfigured_OneDrive_IgnoresFolderLocation()
    {
        AppPreferences.Save(new AppPreferencesData(
            SyncProvider: CloudProvider.OneDrive, SyncLocation: @"C:\sync", OneDriveRootFolderId: null));
        Assert.That(_sut.IsConfigured, Is.False);
    }

    [Test]
    public void IsConfigured_GoogleDriveWithRootFolder_True()
    {
        AppPreferences.Save(new AppPreferencesData(SyncProvider: CloudProvider.GoogleDrive, GoogleDriveRootFolderId: "g-1"));
        Assert.That(_sut.IsConfigured, Is.True);
    }

    [Test]
    public void LocationLabel_Folder_IsThePath()
    {
        AppPreferences.Save(new AppPreferencesData(SyncProvider: CloudProvider.Folder, SyncLocation: @"C:\my\sync"));
        Assert.That(_sut.LocationLabel, Is.EqualTo(@"C:\my\sync"));
    }

    [Test]
    public void LocationLabel_FolderWithoutLocation_FallsBackToProviderName()
    {
        AppPreferences.Save(new AppPreferencesData(SyncProvider: CloudProvider.Folder, SyncLocation: null));
        Assert.That(_sut.LocationLabel, Is.Not.Empty);
    }

    [Test]
    public void LocationLabel_OneDrive_NamesProviderAndFolder()
    {
        AppPreferences.Save(new AppPreferencesData(
            SyncProvider: CloudProvider.OneDrive, OneDriveRootFolderId: "f1", OneDriveRootFolderName: "Collectary"));
        Assert.That(_sut.LocationLabel, Does.Contain("Collectary"));
    }

    [Test]
    public void LocationLabel_GoogleDriveWithoutName_FallsBackToProviderName()
    {
        AppPreferences.Save(new AppPreferencesData(
            SyncProvider: CloudProvider.GoogleDrive, GoogleDriveRootFolderId: "g1", GoogleDriveRootFolderName: null));
        Assert.That(_sut.LocationLabel, Is.Not.Empty);
    }
}
