using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Collectary.Core.Ports;
using Application = Android.App.Application;
using Encoding = Android.Media.Encoding;

namespace Collectary.UI.Android.Audio;

public sealed class AndroidAudioRecorder : IAudioRecorder
{
    private const int SampleRate = 44100;
    private const ChannelIn Channel = ChannelIn.Mono;
    private const Encoding Format = Encoding.Pcm16bit;

    private AudioRecord? _record;
    private MemoryStream? _pcm;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    private AudioManager? GetManager() =>
        Application.Context.GetSystemService(Context.AudioService) as AudioManager;

    public IReadOnlyList<AudioInputDevice> GetInputDevices()
    {
        var devices = new List<AudioInputDevice>();
        var inputs = GetManager()?.GetDevices(GetDevicesTargets.Inputs);
        if (inputs is null) return devices;
        foreach (var device in inputs)
            devices.Add(new AudioInputDevice(device.Id.ToString(), device.ProductName?.ToString() ?? "Microphone"));
        return devices;
    }

    public void Start(string? deviceId)
    {
        var bufferSize = AudioRecord.GetMinBufferSize(SampleRate, Channel, Format);
        _record = new AudioRecord(AudioSource.Mic, SampleRate, Channel, Format, bufferSize);
        TrySelectDevice(deviceId);

        _pcm = new MemoryStream();
        _cts = new CancellationTokenSource();
        _record.StartRecording();

        var token = _cts.Token;
        var record = _record;
        var sink = _pcm;
        _loop = Task.Run(() =>
        {
            var buffer = new byte[bufferSize];
            while (!token.IsCancellationRequested)
            {
                var read = record.Read(buffer, 0, buffer.Length);
                if (read > 0) sink.Write(buffer, 0, read);
            }
        }, token);
    }

    public async Task<RecordedAudio?> StopAsync()
    {
        if (_record is null || _pcm is null || _cts is null || _loop is null) return null;

        _cts.Cancel();
        try { await _loop; } catch (OperationCanceledException) { }

        _record.Stop();
        _record.Release();

        var pcm = _pcm.ToArray();
        _record.Dispose();
        _cts.Dispose();
        _record = null;
        _pcm = null;
        _cts = null;
        _loop = null;

        if (pcm.Length == 0) return null;

        var seconds = (int)Math.Round((double)pcm.Length / (SampleRate * 2));
        return new RecordedAudio(new MemoryStream(WrapInWav(pcm)), seconds);
    }

    private void TrySelectDevice(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId) || !int.TryParse(deviceId, out var id)) return;
        var inputs = GetManager()?.GetDevices(GetDevicesTargets.Inputs);
        if (inputs is null) return;
        foreach (var device in inputs)
            if (device.Id == id)
            {
                _record?.SetPreferredDevice(device);
                return;
            }
    }

    private byte[] WrapInWav(byte[] pcm)
    {
        const int bitsPerSample = 16;
        const int channels = 1;
        var byteRate = SampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + pcm.Length);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(pcm.Length);
        writer.Write(pcm);
        writer.Flush();
        return stream.ToArray();
    }
}
