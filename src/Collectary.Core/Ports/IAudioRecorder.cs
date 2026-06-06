namespace Collectary.Core.Ports;

public sealed record AudioInputDevice(string Id, string Name);

public sealed record RecordedAudio(Stream Data, int DurationSeconds);

public interface IAudioRecorder
{
    IReadOnlyList<AudioInputDevice> GetInputDevices();
    void Start(string? deviceId);
    Task<RecordedAudio?> StopAsync();
}
