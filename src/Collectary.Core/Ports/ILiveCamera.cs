namespace Collectary.Core.Ports;

public sealed record CameraDevice(string Id, string Name);

public sealed record CameraFrame(byte[] JpegBytes, int Width, int Height);

public interface ILiveCamera : IDisposable
{
    IReadOnlyList<CameraDevice> GetDevices();

    /// <summary>
    /// Starts the live preview on <paramref name="deviceId"/> (null = first device). Each captured
    /// frame is delivered to <paramref name="onFrame"/> as JPEG bytes on the UI thread, so callers may
    /// touch bindable state directly. Runs until <paramref name="ct"/> is cancelled or StopAsync is called.
    /// </summary>
    Task StartAsync(string? deviceId, Action<CameraFrame> onFrame, CancellationToken ct);

    Task StopAsync();
}
