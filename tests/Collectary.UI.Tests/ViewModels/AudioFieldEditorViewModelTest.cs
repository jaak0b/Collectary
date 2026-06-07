using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using Glyphs = Collectary.Core.Domain.Fields.IconGlyphs;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class AudioFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(
        IAudioRecorder? recorder = null,
        IAudioPlayer? player = null,
        Func<Stream, Task<string>>? store = null,
        Func<string, Stream?>? open = null,
        string? inputDeviceId = null,
        string? outputDeviceId = null)
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
            ResolveAudioInputDeviceId = () => inputDeviceId,
            ResolveAudioOutputDeviceId = () => outputDeviceId,
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
    public void Ctor_Recorder_AudioAvailable()
    {
        var vm = Make(MakeContext(RecorderWith()));

        Assert.That(vm.AudioAvailable, Is.True);
    }

    [Test]
    public void Ctor_NoRecorder_AudioUnavailable()
    {
        var vm = Make(MakeContext());

        Assert.That(vm.AudioAvailable, Is.False);
    }

    [Test]
    public void RecordTooltip_TellsUserWhereToChangeTheDevice()
    {
        var vm = Make(MakeContext(RecorderWith()));

        Assert.That(vm.RecordTooltip, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task OpenSettings_PersistsTheItemBeforeNavigating()
    {
        var order = new List<string>();
        var ctx = MakeContext(RecorderWith());
        ctx.SaveAsync = () => { order.Add("save"); return Task.CompletedTask; };
        ctx.OpenSettings = () => order.Add("open");
        var vm = Make(ctx);

        await vm.OpenSettingsCommand.ExecuteAsync(null);

        Assert.That(order, Is.EqualTo(new[] { "save", "open" }),
            "the editor must persist pending edits before navigating away to Settings");
    }

    [Test]
    public async Task ToggleRecord_FirstPress_StartsWithConfiguredInputDevice()
    {
        var recorder = RecorderWith();
        var vm = Make(MakeContext(recorder, inputDeviceId: "mic-7"));

        await vm.ToggleRecordCommand.ExecuteAsync(null);

        A.CallTo(() => recorder.Start("mic-7")).MustHaveHappenedOnceExactly();
        Assert.That(vm.IsRecording, Is.True);
    }

    [Test]
    public async Task ToggleRecord_FirstPress_NoConfiguredDevice_StartsWithSystemDefault()
    {
        var recorder = RecorderWith();
        var vm = Make(MakeContext(recorder));

        await vm.ToggleRecordCommand.ExecuteAsync(null);

        A.CallTo(() => recorder.Start(null)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ToggleRecord_RequestsMicrophonePermissionBeforeStarting()
    {
        var recorder = RecorderWith();
        var ctx = MakeContext(recorder, inputDeviceId: "mic-1");
        RuntimePermission? requested = null;
        ctx.RequestPermissionAsync = p => { requested = p; return Task.FromResult(true); };
        var vm = Make(ctx);

        await vm.ToggleRecordCommand.ExecuteAsync(null);

        Assert.That(requested, Is.EqualTo(RuntimePermission.Microphone));
        A.CallTo(() => recorder.Start("mic-1")).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ToggleRecord_WhenMicPermissionDenied_DoesNotStartAndSetsError()
    {
        var recorder = RecorderWith();
        var ctx = MakeContext(recorder, inputDeviceId: "mic-1");
        ctx.RequestPermissionAsync = _ => Task.FromResult(false);
        var vm = Make(ctx);

        await vm.ToggleRecordCommand.ExecuteAsync(null);

        A.CallTo(() => recorder.Start(A<string?>._)).MustNotHaveHappened();
        Assert.Multiple(() =>
        {
            Assert.That(vm.IsRecording, Is.False);
            Assert.That(vm.ErrorMessage, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task ToggleRecord_SecondPress_StoresKeyAndDuration()
    {
        var recorder = RecorderWith();
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
        var recorder = RecorderWith();
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
        var recorder = RecorderWith();
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
    public async Task TogglePlayback_FromIdle_PlaysBlobOnConfiguredOutputDevice()
    {
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._, A<string?>._)).Returns(Task.CompletedTask);
        var opened = new MemoryStream(new byte[] { 9 });
        var vm = Make(MakeContext(player: player, open: _ => opened, outputDeviceId: "out-3"),
            new AudioFieldValue { AudioKey = "k" });

        await vm.TogglePlaybackCommand.ExecuteAsync(null);

        A.CallTo(() => player.PlayAsync(opened, "out-3")).MustHaveHappenedOnceExactly();
        Assert.That(vm.IsPlaying, Is.False);
    }

    [Test]
    public async Task TogglePlayback_NoConfiguredDevice_PlaysOnSystemDefault()
    {
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._, A<string?>._)).Returns(Task.CompletedTask);
        var opened = new MemoryStream(new byte[] { 9 });
        var vm = Make(MakeContext(player: player, open: _ => opened),
            new AudioFieldValue { AudioKey = "k" });

        await vm.TogglePlaybackCommand.ExecuteAsync(null);

        A.CallTo(() => player.PlayAsync(opened, null)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task TogglePlayback_WhilePlaying_Pauses_ThenResumes()
    {
        var tcs = new TaskCompletionSource();
        var player = A.Fake<IAudioPlayer>();
        A.CallTo(() => player.PlayAsync(A<Stream>._, A<string?>._)).Returns(tcs.Task);
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
    public void RecordButtonIcon_IsMicrophoneIdle_StopWhileRecording()
    {
        var vm = Make(MakeContext(RecorderWith()));

        Assert.That(vm.RecordButtonIcon, Is.EqualTo(Glyphs.Microphone));

        vm.IsRecording = true;

        Assert.That(vm.RecordButtonIcon, Is.EqualTo(Glyphs.Stop));
    }

    [Test]
    public void PlayButtonIcon_IsPlayIdle_PauseWhilePlaying_PlayWhenPaused()
    {
        var vm = Make(MakeContext(RecorderWith()));

        Assert.That(vm.PlayButtonIcon, Is.EqualTo(Glyphs.Play));

        vm.IsPlaying = true;
        Assert.That(vm.PlayButtonIcon, Is.EqualTo(Glyphs.Pause), "while playing the button offers to pause");

        vm.IsPaused = true;
        Assert.That(vm.PlayButtonIcon, Is.EqualTo(Glyphs.Play), "while paused the button offers to resume");
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
        A.CallTo(() => player.PlayAsync(A<Stream>._, A<string?>._)).Returns(Task.CompletedTask);
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
        A.CallTo(() => player.PlayAsync(A<Stream>._, A<string?>._)).Throws(new InvalidOperationException("boom"));
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
        A.CallTo(() => player.PlayAsync(A<Stream>._, A<string?>._)).Throws(new InvalidOperationException("boom"));
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
