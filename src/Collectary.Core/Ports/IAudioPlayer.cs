namespace Collectary.Core.Ports;

public sealed record AudioOutputDevice(string Id, string Name);

public interface IAudioPlayer
{
    IReadOnlyList<AudioOutputDevice> GetOutputDevices();
    Task PlayAsync(Stream audio, string? deviceId);
    void Pause();
    void Resume();
    void Stop();
}
