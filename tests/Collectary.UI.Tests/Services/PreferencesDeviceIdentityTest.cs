using Collectary.Presentation.Services;
using Collectary.UI.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class PreferencesDeviceIdentityTest
{
    private string _dir = null!;
    private string _original = null!;

    [SetUp]
    public void SetUp()
    {
        _original = AppPreferences.FilePath;
        _dir = Path.Combine(Path.GetTempPath(), $"collectary-deviceid-{Guid.NewGuid()}");
        Directory.CreateDirectory(_dir);
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _original;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Test]
    public void DeviceId_WhenNonePersisted_GeneratesAndPersistsOnce()
    {
        var sut = new PreferencesDeviceIdentity();

        var id = sut.DeviceId;

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(AppPreferences.Load().DeviceId, Is.EqualTo(id), "a freshly minted device id must be persisted");
        });
    }

    [Test]
    public void DeviceId_IsStableAcrossInstances()
    {
        var first = new PreferencesDeviceIdentity().DeviceId;
        var second = new PreferencesDeviceIdentity().DeviceId;

        Assert.That(second, Is.EqualTo(first), "the device id must survive across resolutions");
    }

    [Test]
    public void DeviceId_WhenAlreadyPersisted_ReturnsStoredValueUnchanged()
    {
        var stored = Guid.NewGuid();
        AppPreferences.Update(p => p with { DeviceId = stored });

        Assert.That(new PreferencesDeviceIdentity().DeviceId, Is.EqualTo(stored));
    }
}
