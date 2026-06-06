using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Collectary.Core.Ports;
using Application = Android.App.Application;
using Stream = System.IO.Stream;

namespace Collectary.UI.Android.Audio;

public sealed class AndroidAudioPlayer : IAudioPlayer
{
    private MediaPlayer? _player;
    private string? _tempPath;

    private AudioManager? GetManager() =>
        Application.Context.GetSystemService(Context.AudioService) as AudioManager;

    public IReadOnlyList<AudioOutputDevice> GetOutputDevices()
    {
        var devices = new List<AudioOutputDevice>();
        var outputs = GetManager()?.GetDevices(GetDevicesTargets.Outputs);
        if (outputs is null) return devices;
        foreach (var device in outputs)
            devices.Add(new AudioOutputDevice(device.Id.ToString(), device.ProductName?.ToString() ?? "Speaker"));
        return devices;
    }

    public async Task PlayAsync(Stream audio, string? deviceId)
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
        TrySelectDevice(deviceId);
        _player.Start();
        await finished.Task;
    }

    private void TrySelectDevice(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId) || !int.TryParse(deviceId, out var id)) return;
        var outputs = GetManager()?.GetDevices(GetDevicesTargets.Outputs);
        if (outputs is null) return;
        foreach (var device in outputs)
            if (device.Id == id)
            {
                _player?.SetPreferredDevice(device);
                return;
            }
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
