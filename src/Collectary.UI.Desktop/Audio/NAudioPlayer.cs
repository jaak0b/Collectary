using Collectary.Core.Ports;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Collectary.UI.Desktop.Audio;

public sealed class NAudioPlayer : IAudioPlayer
{
    private IWavePlayer? _output;
    private MediaFoundationResampler? _resampler;
    private WaveFileReader? _reader;

    public IReadOnlyList<AudioOutputDevice> GetOutputDevices()
    {
        var devices = new List<AudioOutputDevice>();
        using var enumerator = new MMDeviceEnumerator();
        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            devices.Add(new AudioOutputDevice(device.ID, device.FriendlyName));
        return devices;
    }

    public Task PlayAsync(Stream audio, string? deviceId)
    {
        Cleanup();

        _reader = new WaveFileReader(audio);
        _output = CreateOutput(deviceId);
        var finished = new TaskCompletionSource<bool>();

        _output.PlaybackStopped += (_, _) =>
        {
            Cleanup();
            finished.TrySetResult(true);
        };
        _output.Init(_resampler ?? (IWaveProvider)_reader);
        _output.Play();
        return finished.Task;
    }

    private IWavePlayer CreateOutput(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId)) return new WaveOutEvent();

        using var enumerator = new MMDeviceEnumerator();
        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if (device.ID != deviceId) continue;
            var wasapi = new WasapiOut(device, AudioClientShareMode.Shared, useEventSync: false, latency: 200);
            _resampler = new MediaFoundationResampler(_reader!, device.AudioClient.MixFormat);
            return wasapi;
        }

        return new WaveOutEvent();
    }

    public void Pause() => _output?.Pause();

    public void Resume() => _output?.Play();

    public void Stop() => _output?.Stop();

    private void Cleanup()
    {
        _output?.Dispose();
        _resampler?.Dispose();
        _reader?.Dispose();
        _output = null;
        _resampler = null;
        _reader = null;
    }
}
