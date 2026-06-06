using Collectary.Core.Ports;
using NAudio.Wave;

namespace Collectary.UI.Desktop.Audio;

public sealed class NAudioRecorder : IAudioRecorder
{
    private readonly WaveFormat _format = new(44100, 16, 1);
    private WaveInEvent? _waveIn;
    private MemoryStream? _buffer;
    private WaveFileWriter? _writer;
    private TaskCompletionSource<bool>? _stopped;

    public IReadOnlyList<AudioInputDevice> GetInputDevices()
    {
        var devices = new List<AudioInputDevice>();
        for (var n = 0; n < WaveInEvent.DeviceCount; n++)
            devices.Add(new AudioInputDevice(n.ToString(), WaveInEvent.GetCapabilities(n).ProductName));
        return devices;
    }

    public void Start(string? deviceId)
    {
        _buffer = new MemoryStream();
        _writer = new WaveFileWriter(_buffer, _format);
        _stopped = new TaskCompletionSource<bool>();

        _waveIn = new WaveInEvent
        {
            DeviceNumber = int.TryParse(deviceId, out var index) ? index : 0,
            WaveFormat = _format,
        };
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;
        _waveIn.StartRecording();
    }

    public async Task<RecordedAudio?> StopAsync()
    {
        if (_waveIn is null || _buffer is null || _writer is null || _stopped is null) return null;

        _waveIn.StopRecording();
        await _stopped.Task;

        var bytes = _buffer.ToArray();
        var seconds = (int)Math.Round((double)_writer.Length / _format.AverageBytesPerSecond);

        _writer.Dispose();
        _waveIn.Dispose();
        _waveIn = null;
        _buffer = null;
        _writer = null;
        _stopped = null;

        if (bytes.Length == 0) return null;
        return new RecordedAudio(new MemoryStream(bytes), seconds);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e) =>
        _writer?.Write(e.Buffer, 0, e.BytesRecorded);

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _writer?.Flush();
        _stopped?.TrySetResult(true);
    }
}
