namespace Collectary.Core.Ports;

public interface IAudioPlayer
{
    Task PlayAsync(Stream audio);
    void Pause();
    void Resume();
    void Stop();
}
