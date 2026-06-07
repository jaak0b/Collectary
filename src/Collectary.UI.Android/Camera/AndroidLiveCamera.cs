using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Views;
using Avalonia.Threading;
using Collectary.Presentation.Services;
using Application = Android.App.Application;
using PortCameraDevice = Collectary.Core.Ports.CameraDevice;
using PortCameraFrame = Collectary.Core.Ports.CameraFrame;

namespace Collectary.UI.Android.Camera;

public sealed class AndroidLiveCamera : Collectary.Core.Ports.ILiveCamera
{
    private const int CaptureWidth = 640;
    private const int CaptureHeight = 480;

    private readonly CameraManager _manager =
        (CameraManager)Application.Context.GetSystemService(Context.CameraService)!;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _sync = new();

    private bool _active;
    private CameraDevice? _device;
    private CameraCaptureSession? _session;
    private ImageReader? _reader;
    private HandlerThread? _thread;
    private Handler? _handler;
    private Action<PortCameraFrame>? _onFrame;
    private CancellationTokenRegistration _ctRegistration;

    public IReadOnlyList<PortCameraDevice> GetDevices()
    {
        var devices = new List<PortCameraDevice>();
        foreach (var id in _manager.GetCameraIdList())
        {
            var characteristics = _manager.GetCameraCharacteristics(id);
            var facing = characteristics.Get(CameraCharacteristics.LensFacing) as Java.Lang.Integer;
            var name = facing?.IntValue() switch
            {
                (int)LensFacing.Front => "Front camera",
                (int)LensFacing.Back => "Back camera",
                _ => $"Camera {id}"
            };
            devices.Add(new PortCameraDevice(id, name));
        }
        return devices;
    }

    public async Task StartAsync(string? deviceId, Action<PortCameraFrame> onFrame, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            StopCurrentSession();
            ct.ThrowIfCancellationRequested();

            var id = deviceId ?? _manager.GetCameraIdList().FirstOrDefault();
            if (id is null) return;

            if (Application.Context.CheckSelfPermission(global::Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                AppLogger.Log.Error("Camera permission has not been granted");
                throw new InvalidOperationException("Camera permission is not granted.");
            }

            _thread = new HandlerThread("CollectaryCamera");
            _thread.Start();
            _handler = new Handler(_thread.Looper!);
            _reader = ImageReader.NewInstance(CaptureWidth, CaptureHeight, ImageFormatType.Yuv420888, 2);
            _reader.SetOnImageAvailableListener(new FrameListener(this), _handler);
            _onFrame = onFrame;
            _ctRegistration = ct.Register(() => _ = StopAsync());

            lock (_sync)
                _active = true;

            try
            {
                _manager.OpenCamera(id, new DeviceCallback(this), _handler);
            }
            catch (Exception ex)
            {
                AppLogger.Log.Error(ex, "Opening the camera failed");
                StopCurrentSession();
                throw new InvalidOperationException("The camera could not be opened.", ex);
            }
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
            StopCurrentSession();
        }
        finally
        {
            _gate.Release();
        }
    }

    private void StopCurrentSession()
    {
        lock (_sync)
        {
            _active = false;
            _onFrame = null;

            try
            {
                _session?.StopRepeating();
            }
            catch (Exception ex)
            {
                AppLogger.Log.Error(ex, "Stopping camera preview failed");
            }

            _session?.Close();
            _session = null;
            _device?.Close();
            _device = null;
            _reader?.Close();
            _reader = null;
            _ctRegistration.Dispose();
            _ctRegistration = default;
            _thread?.QuitSafely();
            _thread = null;
            _handler = null;
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _gate.Dispose();
    }

    private void OnDeviceOpened(CameraDevice camera)
    {
        lock (_sync)
        {
            if (!_active || _reader is null || _handler is null)
            {
                camera.Close();
                return;
            }

            _device = camera;
            var surface = _reader.Surface!;
            camera.CreateCaptureSession(new List<Surface> { surface }, new SessionCallback(this, surface), _handler);
        }
    }

    private void OnDeviceLost(CameraDevice camera)
    {
        lock (_sync)
        {
            camera.Close();
            if (ReferenceEquals(_device, camera))
                _device = null;
        }
    }

    private void OnSessionConfigured(CameraCaptureSession session, Surface surface)
    {
        lock (_sync)
        {
            if (!_active || _device is null)
            {
                session.Close();
                return;
            }

            _session = session;
            var request = _device.CreateCaptureRequest(CameraTemplate.Preview);
            request.AddTarget(surface);
            session.SetRepeatingRequest(request.Build(), null, _handler);
        }
    }

    private void OnImageAvailable(ImageReader reader)
    {
        try
        {
            using var image = reader.AcquireLatestImage();
            if (image is null) return;

            Action<PortCameraFrame>? sink;
            lock (_sync)
                sink = _active ? _onFrame : null;
            if (sink is null) return;

            var jpeg = ToJpeg(image);
            var frame = new PortCameraFrame(jpeg, image.Width, image.Height);
            Dispatcher.UIThread.Post(() => sink(frame));
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Reading a camera frame failed");
        }
    }

    private byte[] ToJpeg(Image image)
    {
        var nv21 = YuvToNv21(image);
        using var yuv = new YuvImage(nv21, ImageFormatType.Nv21, image.Width, image.Height, null);
        using var stream = new MemoryStream();
        yuv.CompressToJpeg(new Rect(0, 0, image.Width, image.Height), 80, stream);
        return stream.ToArray();
    }

    private byte[] YuvToNv21(Image image)
    {
        var width = image.Width;
        var height = image.Height;
        var ySize = width * height;
        var nv21 = new byte[ySize + ySize / 2];
        var planes = image.GetPlanes()!;

        var yBuffer = planes[0].Buffer!;
        var yRowStride = planes[0].RowStride;
        var position = 0;
        if (yRowStride == width)
        {
            yBuffer.Get(nv21, 0, ySize);
            position = ySize;
        }
        else
        {
            for (var row = 0; row < height; row++)
            {
                yBuffer.Position(row * yRowStride);
                yBuffer.Get(nv21, position, width);
                position += width;
            }
        }

        var uBuffer = planes[1].Buffer!;
        var vBuffer = planes[2].Buffer!;
        var uvRowStride = planes[1].RowStride;
        var uvPixelStride = planes[1].PixelStride;
        var uLimit = uBuffer.Limit();
        var vLimit = vBuffer.Limit();
        for (var row = 0; row < height / 2; row++)
        {
            for (var col = 0; col < width / 2; col++)
            {
                var offset = row * uvRowStride + col * uvPixelStride;
                nv21[position++] = offset < vLimit ? (byte)vBuffer.Get(offset) : (byte)0;
                nv21[position++] = offset < uLimit ? (byte)uBuffer.Get(offset) : (byte)0;
            }
        }
        return nv21;
    }

    private sealed class DeviceCallback : CameraDevice.StateCallback
    {
        private readonly AndroidLiveCamera _owner;
        public DeviceCallback(AndroidLiveCamera owner) => _owner = owner;
        public override void OnOpened(CameraDevice camera) => _owner.OnDeviceOpened(camera);
        public override void OnDisconnected(CameraDevice camera) => _owner.OnDeviceLost(camera);
        public override void OnError(CameraDevice camera, CameraError error) => _owner.OnDeviceLost(camera);
    }

    private sealed class SessionCallback : CameraCaptureSession.StateCallback
    {
        private readonly AndroidLiveCamera _owner;
        private readonly Surface _surface;
        public SessionCallback(AndroidLiveCamera owner, Surface surface)
        {
            _owner = owner;
            _surface = surface;
        }

        public override void OnConfigured(CameraCaptureSession session) =>
            _owner.OnSessionConfigured(session, _surface);

        public override void OnConfigureFailed(CameraCaptureSession session) =>
            AppLogger.Log.Error("Camera capture session could not be configured");
    }

    private sealed class FrameListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        private readonly AndroidLiveCamera _owner;
        public FrameListener(AndroidLiveCamera owner) => _owner = owner;
        public void OnImageAvailable(ImageReader? reader)
        {
            if (reader is not null) _owner.OnImageAvailable(reader);
        }
    }
}
