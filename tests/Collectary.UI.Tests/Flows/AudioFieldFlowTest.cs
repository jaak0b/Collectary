using Avalonia.Threading;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Storage;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using FakeItEasy;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class AudioFieldFlowTest
{
    private string _dir = null!;
    private FileSystemImageStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _store = new FileSystemImageStore(_dir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private ItemEditingContext MakeContext(IAudioRecorder recorder, IAudioPlayer player) => new(
        editorRegistry: A.Fake<IFieldEditorRegistry>(),
        listCellBuilder: A.Fake<IListCellBuilder>(),
        goBack: () => { },
        pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
        exportImageAsync: (_, _) => Task.CompletedTask,
        loadImageBitmap: _ => null,
        deleteImageAsync: _ => Task.CompletedTask)
    {
        AudioRecorder = recorder,
        AudioPlayer = player,
        StoreAudioAsync = stream => _store.SaveAsync(stream, $"audio-{Guid.NewGuid():N}.wav"),
        OpenAudioStream = key => _store.Exists(key) ? _store.Open(key) : null,
    };

    private static void Pump(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }

        if (!task.IsCompleted)
            throw new TimeoutException("Command did not complete while pumping the dispatcher.");
        task.GetAwaiter().GetResult();
    }

    [Test]
    public void RecordThenStop_PersistsBlob_AndReloadsAsHasAudio()
    {
        var clip = new byte[] { 0x52, 0x49, 0x46, 0x46, 9, 8, 7 };
        var recorder = A.Fake<IAudioRecorder>();
        A.CallTo(() => recorder.GetInputDevices()).Returns(new[] { new AudioInputDevice("m1", "Mic") });
        A.CallTo(() => recorder.StopAsync()).Returns(new RecordedAudio(new MemoryStream(clip), 5));
        var ctx = MakeContext(recorder, A.Fake<IAudioPlayer>());

        var vm = new AudioFieldEditorViewModel(new AudioFieldDefinition(), new AudioFieldValue(), ctx);
        Pump(vm.ToggleRecordCommand.ExecuteAsync(null));
        Pump(vm.ToggleRecordCommand.ExecuteAsync(null));

        var saved = (AudioFieldValue)vm.GetCurrentValue();
        Assert.Multiple(() =>
        {
            Assert.That(saved.AudioKey, Is.Not.Null.And.Not.Empty);
            Assert.That(_store.Exists(saved.AudioKey!), Is.True);
            Assert.That(saved.DurationSeconds, Is.EqualTo(5));
        });

        var reloaded = new AudioFieldEditorViewModel(new AudioFieldDefinition(), saved, ctx);
        Assert.That(reloaded.HasAudio, Is.True);
    }

    [Test]
    public void Playback_OpensStoredBlobAndHandsItToPlayer()
    {
        var clip = new byte[] { 1, 2, 3, 4, 5 };
        var recorder = A.Fake<IAudioRecorder>();
        A.CallTo(() => recorder.StopAsync()).Returns(new RecordedAudio(new MemoryStream(clip), 2));
        byte[]? played = null;
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._)).Invokes((Stream s) =>
        {
            using var buffer = new MemoryStream();
            s.CopyTo(buffer);
            played = buffer.ToArray();
        }).Returns(Task.CompletedTask);
        var ctx = MakeContext(recorder, player);

        var vm = new AudioFieldEditorViewModel(new AudioFieldDefinition(), new AudioFieldValue(), ctx);
        Pump(vm.ToggleRecordCommand.ExecuteAsync(null));
        Pump(vm.ToggleRecordCommand.ExecuteAsync(null));
        Pump(vm.TogglePlaybackCommand.ExecuteAsync(null));

        Assert.That(played, Is.EqualTo(clip));
    }
}
