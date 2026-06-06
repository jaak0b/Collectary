using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using FakeItEasy;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelAudioTest
{
    private string _dir = null!;
    private string _original = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _original = AppPreferences.FilePath;
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
        LocalizationService.Instance.Apply("en");
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _original;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static IAudioRecorder RecorderWith(params AudioInputDevice[] devices)
    {
        var recorder = A.Fake<IAudioRecorder>();
        A.CallTo(() => recorder.GetInputDevices()).Returns(devices);
        return recorder;
    }

    private static IAudioPlayer PlayerWith(params AudioOutputDevice[] devices)
    {
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.GetOutputDevices()).Returns(devices);
        return player;
    }

    private static SettingsViewModel Make(IAudioRecorder? recorder = null, IAudioPlayer? player = null) =>
        new(() => { }, audioRecorder: recorder, audioPlayer: player);

    [Test]
    public void InputDevices_SystemDefaultFirst_ThenEnumeratedMicrophones()
    {
        var sut = Make(RecorderWith(new AudioInputDevice("1", "Built-in"), new AudioInputDevice("2", "USB")));

        Assert.Multiple(() =>
        {
            Assert.That(sut.InputDevices[0].Id, Is.Null, "the first option is the system default");
            Assert.That(sut.InputDevices[0].Name, Is.EqualTo(LocalizationService.Instance["Audio_SystemDefault"]));
            Assert.That(sut.InputDevices.Select(o => o.Id), Is.EqualTo(new[] { null, "1", "2" }));
            Assert.That(sut.InputDevices.Select(o => o.Name), Does.Contain("USB"));
        });
    }

    [Test]
    public void OutputDevices_SystemDefaultFirst_ThenEnumeratedSpeakers()
    {
        var sut = Make(player: PlayerWith(new AudioOutputDevice("9", "Speakers")));

        Assert.Multiple(() =>
        {
            Assert.That(sut.OutputDevices[0].Id, Is.Null);
            Assert.That(sut.OutputDevices.Select(o => o.Id), Is.EqualTo(new[] { null, "9" }));
        });
    }

    [Test]
    public void ShowAudioSettings_FalseWithoutPorts_TrueWithAPort()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Make().ShowAudioSettings, Is.False);
            Assert.That(Make(RecorderWith()).ShowAudioSettings, Is.True);
            Assert.That(Make(player: PlayerWith()).ShowAudioSettings, Is.True);
        });
    }

    [Test]
    public void SelectedInputDevice_DefaultsToSystemDefault_WhenNothingSaved()
    {
        var sut = Make(RecorderWith(new AudioInputDevice("1", "Built-in")));

        Assert.That(sut.SelectedInputDevice!.Id, Is.Null);
    }

    [Test]
    public void SelectedInputDevice_ReflectsSavedPreference()
    {
        AppPreferences.Update(p => p with { AudioInputDeviceId = "2" });

        var sut = Make(RecorderWith(new AudioInputDevice("1", "Built-in"), new AudioInputDevice("2", "USB")));

        Assert.That(sut.SelectedInputDevice!.Id, Is.EqualTo("2"));
    }

    [Test]
    public void ChangingInputDevice_PersistsItsId()
    {
        var sut = Make(RecorderWith(new AudioInputDevice("1", "Built-in"), new AudioInputDevice("2", "USB")));

        sut.SelectedInputDevice = sut.InputDevices.First(o => o.Id == "2");

        Assert.That(AppPreferences.Load().AudioInputDeviceId, Is.EqualTo("2"));
    }

    [Test]
    public void ChangingBackToSystemDefault_PersistsNull()
    {
        AppPreferences.Update(p => p with { AudioInputDeviceId = "2" });
        var sut = Make(RecorderWith(new AudioInputDevice("1", "Built-in"), new AudioInputDevice("2", "USB")));

        sut.SelectedInputDevice = sut.InputDevices.First(o => o.Id is null);

        Assert.That(AppPreferences.Load().AudioInputDeviceId, Is.Null);
    }

    [Test]
    public void ChangingOutputDevice_PersistsItsId()
    {
        var sut = Make(player: PlayerWith(new AudioOutputDevice("9", "Speakers")));

        sut.SelectedOutputDevice = sut.OutputDevices.First(o => o.Id == "9");

        Assert.That(AppPreferences.Load().AudioOutputDeviceId, Is.EqualTo("9"));
    }

    [Test]
    public void SavedInputDeviceNoLongerPresent_FallsBackToSystemDefault()
    {
        AppPreferences.Update(p => p with { AudioInputDeviceId = "unplugged" });

        var sut = Make(RecorderWith(new AudioInputDevice("1", "Built-in")));

        Assert.That(sut.SelectedInputDevice!.Id, Is.Null);
    }

    [Test]
    public void SavedOutputDeviceNoLongerPresent_FallsBackToSystemDefault()
    {
        AppPreferences.Update(p => p with { AudioOutputDeviceId = "unplugged" });

        var sut = Make(player: PlayerWith(new AudioOutputDevice("9", "Speakers")));

        Assert.That(sut.SelectedOutputDevice!.Id, Is.Null);
    }

    [Test]
    public void ClearingTheSelection_DoesNotPersist()
    {
        AppPreferences.Update(p => p with { AudioInputDeviceId = "2" });
        var sut = Make(RecorderWith(new AudioInputDevice("1", "Built-in"), new AudioInputDevice("2", "USB")));

        sut.SelectedInputDevice = null;

        Assert.That(AppPreferences.Load().AudioInputDeviceId, Is.EqualTo("2"), "a null selection must leave the saved device untouched");
    }
}
