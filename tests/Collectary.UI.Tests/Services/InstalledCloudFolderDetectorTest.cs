using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class InstalledCloudFolderDetectorTest
{
    private readonly InstalledCloudFolderDetector _sut = new();
    private string? _origConsumer;
    private string? _origOneDrive;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _origConsumer = Environment.GetEnvironmentVariable("OneDriveConsumer");
        _origOneDrive = Environment.GetEnvironmentVariable("OneDrive");
        _tempDir = Path.Combine(Path.GetTempPath(), "collectary-detector-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        Environment.SetEnvironmentVariable("OneDriveConsumer", null);
        Environment.SetEnvironmentVariable("OneDrive", null);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("OneDriveConsumer", _origConsumer);
        Environment.SetEnvironmentVariable("OneDrive", _origOneDrive);
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public void Detect_WhenConsumerFolderExists_ReturnsIt()
    {
        Environment.SetEnvironmentVariable("OneDriveConsumer", _tempDir);

        Assert.That(_sut.Detect(), Is.EqualTo(_tempDir));
    }

    [Test]
    public void Detect_WhenOnlyOneDriveSet_FallsBackToIt()
    {
        Environment.SetEnvironmentVariable("OneDrive", _tempDir);

        Assert.That(_sut.Detect(), Is.EqualTo(_tempDir));
    }

    [Test]
    public void Detect_WhenVariablesPointToMissingFolder_ReturnsNull()
    {
        Environment.SetEnvironmentVariable("OneDriveConsumer", Path.Combine(_tempDir, "does-not-exist"));

        Assert.That(_sut.Detect(), Is.Null);
    }

    [Test]
    public void Detect_WhenNothingSet_ReturnsNull() => Assert.That(_sut.Detect(), Is.Null);
}
