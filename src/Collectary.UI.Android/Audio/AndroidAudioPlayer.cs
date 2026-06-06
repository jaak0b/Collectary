using System;
using System.IO;
using System.Threading.Tasks;
using Android.Media;
using Collectary.Core.Ports;
using Application = Android.App.Application;
using Stream = System.IO.Stream;

namespace Collectary.UI.Android.Audio;

public sealed class AndroidAudioPlayer : IAudioPlayer
{
    private MediaPlayer? _player;
    private string? _tempPath;

    public async Task PlayAsync(Stream audio)
    {
        Cleanup();

        _tempPath = Path.Combine(Application.Context.CacheDir!.AbsolutePath, $"play-{Guid.NewGuid():N}.wav");
        await using (var file = File.Create(_tempPath))
            await audio.CopyToAsync(file);

        var finished = new TaskCompletionSource<bool>();
        _player = new MediaPlayer();
        _player.Completion += (_, _) =>
        {
            Cleanup();
            finished.TrySetResult(true);
        };
        _player.SetDataSource(_tempPath);
        _player.Prepare();
        _player.Start();
        await finished.Task;
    }

    public void Pause() => _player?.Pause();

    public void Resume() => _player?.Start();

    public void Stop() => _player?.Stop();

    private void Cleanup()
    {
        _player?.Release();
        _player?.Dispose();
        _player = null;
        if (_tempPath is not null && File.Exists(_tempPath))
        {
            try { File.Delete(_tempPath); } catch (IOException) { }
        }
        _tempPath = null;
    }
}
