using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class AudioFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(
        IAudioRecorder? recorder = null,
        IAudioPlayer? player = null,
        Func<Stream, Task<string>>? store = null,
        Func<string, Stream?>? open = null)
    {
        var ctx = new ItemEditingContext(
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
        };
        if (store is not null) ctx.StoreAudioAsync = store;
        if (open is not null) ctx.OpenAudioStream = open;
        return ctx;
    }

    private static IAudioRecorder RecorderWith(params AudioInputDevice[] devices)
    {
        var recorder = A.Fake<IAudioRecorder>();
        A.CallTo(() => recorder.GetInputDevices()).Returns(devices);
        return recorder;
    }

    private static AudioFieldEditorViewModel Make(ItemEditingContext ctx, AudioFieldValue? value = null) =>
        new(new AudioFieldDefinition(), value ?? new AudioFieldValue(), ctx);

    [Test]
    public void Ctor_PopulatesMicrophonesAndSelectsFirst()
    {
        var mic1 = new AudioInputDevice("1", "Built-in");
        var mic2 = new AudioInputDevice("2", "USB");
        var vm = Make(MakeContext(RecorderWith(mic1, mic2)));

        Assert.Multiple(() =>
        {
            Assert.That(vm.Microphones, Is.EqualTo(new[] { mic1, mic2 }));
            Assert.That(vm.SelectedMicrophone, Is.EqualTo(mic1));
            Assert.That(vm.AudioAvailable, Is.True);
        });
    }

    [Test]
    public void Ctor_NoRecorder_AudioUnavailable()
    {
        var vm = Make(MakeContext());

        Assert.Multiple(() =>
        {
            Assert.That(vm.AudioAvailable, Is.False);
            Assert.That(vm.Microphones, Is.Empty);
        });
    }

    [Test]
    public async Task ToggleRecord_FirstPress_StartsWithSelectedMic()
    {
        var recorder = RecorderWith(new AudioInputDevice("mic-7", "USB"));
        var vm = Make(MakeContext(recorder));

        await vm.ToggleRecordCommand.ExecuteAsync(null);

        A.CallTo(() => recorder.Start("mic-7")).MustHaveHappenedOnceExactly();
        Assert.That(vm.IsRecording, Is.True);
    }

    [Test]
    public async Task ToggleRecord_SecondPress_StoresKeyAndDuration()
    {
        var recorder = RecorderWith(new AudioInputDevice("mic-1", "Built-in"));
        A.CallTo(() => recorder.StopAsync())
            .Returns(new RecordedAudio(new MemoryStream(new byte[] { 1, 2, 3 }), 12));
        var vm = Make(MakeContext(recorder, store: _ => Task.FromResult("audio-99")));

        await vm.ToggleRecordCommand.ExecuteAsync(null);
        await vm.ToggleRecordCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsRecording, Is.False);
            Assert.That(vm.AudioKey, Is.EqualTo("audio-99"));
            Assert.That(vm.DurationSeconds, Is.EqualTo(12));
            Assert.That(vm.HasAudio, Is.True);
        });
        var stored = (AudioFieldValue)vm.GetCurrentValue();
        Assert.That(stored.AudioKey, Is.EqualTo("audio-99"));
        Assert.That(stored.DurationSeconds, Is.EqualTo(12));
    }

    [Test]
    public async Task ToggleRecord_StopWithNoCapture_LeavesEmpty()
    {
        var recorder = RecorderWith(new AudioInputDevice("mic-1", "Built-in"));
        A.CallTo(() => recorder.StopAsync()).Returns(Task.FromResult<RecordedAudio?>(null));
        var vm = Make(MakeContext(recorder));

        await vm.ToggleRecordCommand.ExecuteAsync(null);
        await vm.ToggleRecordCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsRecording, Is.False);
            Assert.That(vm.HasAudio, Is.False);
        });
    }

    [Test]
    public async Task ToggleRecord_RecorderThrows_SetsErrorMessageAndStops()
    {
        var recorder = RecorderWith(new AudioInputDevice("mic-1", "Built-in"));
        A.CallTo(() => recorder.Start(A<string?>._)).Throws(new InvalidOperationException("boom"));
        var vm = Make(MakeContext(recorder));

        await vm.ToggleRecordCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsRecording, Is.False);
            Assert.That(vm.ErrorMessage, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task TogglePlayback_FromIdle_PlaysBlobAndResetsOnCompletion()
    {
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._)).Returns(Task.CompletedTask);
        var opened = new MemoryStream(new byte[] { 9 });
        var vm = Make(MakeContext(player: player, open: _ => opened),
            new AudioFieldValue { AudioKey = "k" });

        await vm.TogglePlaybackCommand.ExecuteAsync(null);

        A.CallTo(() => player.PlayAsync(opened)).MustHaveHappenedOnceExactly();
        Assert.That(vm.IsPlaying, Is.False);
    }

    [Test]
    public async Task TogglePlayback_WhilePlaying_Pauses_ThenResumes()
    {
        var tcs = new TaskCompletionSource();
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._)).Returns(tcs.Task);
        var vm = Make(MakeContext(player: player, open: _ => new MemoryStream(new byte[] { 1 })),
            new AudioFieldValue { AudioKey = "k" });

        var playing = vm.TogglePlaybackCommand.ExecuteAsync(null);
        Assert.That(vm.IsPlaying, Is.True);

        await vm.TogglePlaybackCommand.ExecuteAsync(null);
        A.CallTo(() => player.Pause()).MustHaveHappenedOnceExactly();
        Assert.That(vm.IsPaused, Is.True);

        await vm.TogglePlaybackCommand.ExecuteAsync(null);
        A.CallTo(() => player.Resume()).MustHaveHappenedOnceExactly();
        Assert.That(vm.IsPaused, Is.False);

        tcs.SetResult();
        await playing;
        Assert.That(vm.IsPlaying, Is.False);
    }

    [Test]
    public void RecordButtonLabel_ReflectsRecordingState()
    {
        var vm = Make(MakeContext(RecorderWith()));
        var idle = vm.RecordButtonLabel;

        vm.IsRecording = true;

        Assert.Multiple(() =>
        {
            Assert.That(idle, Is.Not.Empty);
            Assert.That(vm.RecordButtonLabel, Is.Not.Empty.And.Not.EqualTo(idle));
        });
    }

    [Test]
    public void PlayButtonLabel_ReflectsPlayingAndPausedState()
    {
        var vm = Make(MakeContext(RecorderWith()));
        var idle = vm.PlayButtonLabel;

        vm.IsPlaying = true;
        var playing = vm.PlayButtonLabel;
        vm.IsPaused = true;

        Assert.Multiple(() =>
        {
            Assert.That(playing, Is.Not.EqualTo(idle));
            Assert.That(vm.PlayButtonLabel, Is.EqualTo(idle));
        });
    }

    [Test]
    public async Task TogglePlayback_DisposesOpenedStreamAfterPlayback()
    {
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._)).Returns(Task.CompletedTask);
        var opened = new DisposeTrackingStream(new byte[] { 9 });
        var vm = Make(MakeContext(player: player, open: _ => opened),
            new AudioFieldValue { AudioKey = "k" });

        await vm.TogglePlaybackCommand.ExecuteAsync(null);

        Assert.That(opened.Disposed, Is.True);
    }

    [Test]
    public async Task TogglePlayback_PlayerThrows_DisposesOpenedStream()
    {
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._)).Throws(new InvalidOperationException("boom"));
        var opened = new DisposeTrackingStream(new byte[] { 9 });
        var vm = Make(MakeContext(player: player, open: _ => opened),
            new AudioFieldValue { AudioKey = "k" });

        await vm.TogglePlaybackCommand.ExecuteAsync(null);

        Assert.That(opened.Disposed, Is.True);
    }

    private sealed class DisposeTrackingStream(byte[] data) : MemoryStream(data)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    [Test]
    public async Task TogglePlayback_PlayerThrows_SetsErrorMessage()
    {
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._)).Throws(new InvalidOperationException("boom"));
        var vm = Make(MakeContext(player: player, open: _ => new MemoryStream(new byte[] { 1 })),
            new AudioFieldValue { AudioKey = "k" });

        await vm.TogglePlaybackCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsPlaying, Is.False);
            Assert.That(vm.ErrorMessage, Is.Not.Null.And.Not.Empty);
        });
    }
}
