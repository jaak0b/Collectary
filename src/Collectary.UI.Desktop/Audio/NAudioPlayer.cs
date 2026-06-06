using Collectary.Core.Ports;
using NAudio.Wave;

namespace Collectary.UI.Desktop.Audio;

public sealed class NAudioPlayer : IAudioPlayer
{
    private WaveOutEvent? _output;
    private WaveFileReader? _reader;

    public Task PlayAsync(Stream audio)
    {
        Cleanup();

        _reader = new WaveFileReader(audio);
        _output = new WaveOutEvent();
        var finished = new TaskCompletionSource<bool>();

        _output.PlaybackStopped += (_, _) =>
        {
            Cleanup();
            finished.TrySetResult(true);
        };
        _output.Init(_reader);
        _output.Play();
        return finished.Task;
    }

    public void Pause() => _output?.Pause();

    public void Resume() => _output?.Play();

    public void Stop() => _output?.Stop();

    private void Cleanup()
    {
        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }
}
