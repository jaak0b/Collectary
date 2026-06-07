using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using OpenCvSharp;

namespace Collectary.UI.Desktop.Camera;

public sealed class DesktopLiveCamera : ILiveCamera
{
    private const int ProbeLimit = 5;
    private const int FrameDelayMs = 66;
    private const VideoCaptureAPIs Backend = VideoCaptureAPIs.DSHOW;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _devicesLock = new();
    private IReadOnlyList<CameraDevice>? _devices;

    private VideoCapture? _capture;
    private CancellationTokenSource? _loopCts;
    private Task? _loop;

    public IReadOnlyList<CameraDevice> GetDevices()
    {
        lock (_devicesLock)
            return _devices ??= ProbeDevices();
    }

    private IReadOnlyList<CameraDevice> ProbeDevices()
    {
        var devices = new List<CameraDevice>();
        for (var index = 0; index < ProbeLimit; index++)
        {
            try
            {
                using var probe = new VideoCapture(index, Backend);
                if (!probe.IsOpened()) break;
                devices.Add(new CameraDevice(
                    index.ToString(CultureInfo.InvariantCulture),
                    $"Camera {index + 1}"));
            }
            catch (Exception ex)
            {
                AppLogger.Log.Error(ex, "Probing camera index {Index} failed", index);
                break;
            }
        }
        return devices;
    }

    public async Task StartAsync(string? deviceId, Action<CameraFrame> onFrame, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await StopCurrentSessionAsync().ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            var index = int.TryParse(deviceId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed : 0;

            var capture = await Task.Run(() => new VideoCapture(index, Backend), ct).ConfigureAwait(false);
            if (!capture.IsOpened())
            {
                capture.Dispose();
                throw new InvalidOperationException($"Camera index {index} could not be opened.");
            }

            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _loopCts.Token;
            _capture = capture;
            _loop = Task.Run(() => CaptureLoop(capture, onFrame, token));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await StopCurrentSessionAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task StopCurrentSessionAsync()
    {
        _loopCts?.Cancel();
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppLogger.Log.Error(ex, "Camera capture loop faulted while stopping");
            }
        }
        _loop = null;
        _loopCts?.Dispose();
        _loopCts = null;
        if (_capture is not null)
        {
            _capture.Release();
            _capture.Dispose();
            _capture = null;
        }
    }

    private void CaptureLoop(VideoCapture capture, Action<CameraFrame> onFrame, CancellationToken token)
    {
        using var frame = new Mat();
        while (!token.IsCancellationRequested)
        {
            if (!capture.Read(frame) || frame.Empty())
            {
                Thread.Sleep(FrameDelayMs);
                continue;
            }

            Cv2.ImEncode(".jpg", frame, out var jpeg);
            var captured = new CameraFrame(jpeg, frame.Width, frame.Height);
            Dispatcher.UIThread.Post(() => onFrame(captured));
            Thread.Sleep(FrameDelayMs);
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _gate.Dispose();
    }
}
