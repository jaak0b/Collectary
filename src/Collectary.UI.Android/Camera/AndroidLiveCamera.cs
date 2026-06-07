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
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Avalonia.Threading;
using Collectary.Presentation.Services;
using Java.Util.Concurrent;
using Application = Android.App.Application;
using PortCameraDevice = Collectary.Core.Ports.CameraDevice;
using PortCameraFrame = Collectary.Core.Ports.CameraFrame;

namespace Collectary.UI.Android.Camera;

public sealed class AndroidLiveCamera : Collectary.Core.Ports.ILiveCamera
{
    private readonly CameraManager _manager =
        (CameraManager)Application.Context.GetSystemService(Context.CameraService)!;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _sync = new();
    private readonly IExecutor _analysisExecutor = Executors.NewSingleThreadExecutor()!;
    private readonly CameraLifecycleOwner _lifecycleOwner = new();

    private bool _active;
    private ProcessCameraProvider? _provider;
    private ImageAnalysis? _analysis;
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
            await StopCurrentSessionAsync().ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            if (Application.Context.CheckSelfPermission(global::Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                AppLogger.Log.Error("Camera permission has not been granted");
                throw new InvalidOperationException("Camera permission is not granted.");
            }

            lock (_sync)
            {
                _onFrame = onFrame;
                _active = true;
            }
            _ctRegistration = ct.Register(() => _ = StopAsync());

            await BindAsync(deviceId).ConfigureAwait(false);
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

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _gate.Dispose();
    }

    private Task BindAsync(string? deviceId)
    {
        var ctx = Application.Context;
        var future = ProcessCameraProvider.GetInstance(ctx);
        var tcs = new TaskCompletionSource();
        future.AddListener(new Java.Lang.Runnable(() =>
        {
            try
            {
                var provider = (ProcessCameraProvider)future.Get()!;
                _provider = provider;
                _analysis = new ImageAnalysis.Builder()
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                    .Build();
                _analysis.SetAnalyzer(_analysisExecutor, new FrameAnalyzer(this));

                _lifecycleOwner.MarkResumed();
                provider.UnbindAll();
                provider.BindToLifecycle(_lifecycleOwner, BuildSelector(deviceId), _analysis);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                AppLogger.Log.Error(ex, "Opening the camera failed");
                tcs.TrySetException(new InvalidOperationException("The camera could not be opened.", ex));
            }
        }), ContextCompat.GetMainExecutor(ctx)!);
        return tcs.Task;
    }

    private Task StopCurrentSessionAsync()
    {
        lock (_sync)
        {
            _active = false;
            _onFrame = null;
        }
        _ctRegistration.Dispose();
        _ctRegistration = default;

        var provider = _provider;
        var analysis = _analysis;
        _provider = null;
        _analysis = null;
        if (provider is null && analysis is null) return Task.CompletedTask;

        var tcs = new TaskCompletionSource();
        ContextCompat.GetMainExecutor(Application.Context)!.Execute(new Java.Lang.Runnable(() =>
        {
            try
            {
                analysis?.ClearAnalyzer();
                provider?.UnbindAll();
                _lifecycleOwner.MarkStopped();
            }
            catch (Exception ex)
            {
                AppLogger.Log.Error(ex, "Stopping the camera failed");
            }
            finally
            {
                tcs.TrySetResult();
            }
        }));
        return tcs.Task;
    }

    private CameraSelector BuildSelector(string? deviceId) =>
        new CameraSelector.Builder().RequireLensFacing(ResolveLensFacing(deviceId)).Build();

    private int ResolveLensFacing(string? deviceId)
    {
        try
        {
            if (!string.IsNullOrEmpty(deviceId))
            {
                var facing = _manager.GetCameraCharacteristics(deviceId)
                    .Get(CameraCharacteristics.LensFacing) as Java.Lang.Integer;
                if (facing?.IntValue() == (int)LensFacing.Front)
                    return CameraSelector.LensFacingFront;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Resolving the camera lens facing failed");
        }
        return CameraSelector.LensFacingBack;
    }

    private void OnAnalyze(IImageProxy proxy)
    {
        try
        {
            Action<PortCameraFrame>? sink;
            lock (_sync)
                sink = _active ? _onFrame : null;

            var image = proxy.Image;
            if (sink is null || image is null) return;

            var width = image.Width;
            var height = image.Height;
            var nv21 = YuvToNv21(image);
            var jpeg = Nv21ToJpeg(nv21, width, height);
            Dispatcher.UIThread.Post(() => sink(new PortCameraFrame(jpeg, width, height)));
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Reading a camera frame failed");
        }
        finally
        {
            proxy.Close();
        }
    }

    private byte[] Nv21ToJpeg(byte[] nv21, int width, int height)
    {
        using var yuv = new YuvImage(nv21, ImageFormatType.Nv21, width, height, null);
        using var stream = new MemoryStream();
        yuv.CompressToJpeg(new Rect(0, 0, width, height), 80, stream);
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

        var uvRowStride = planes[1].RowStride;
        var uvPixelStride = planes[1].PixelStride;

        var uBuffer = planes[1].Buffer!;
        var vBuffer = planes[2].Buffer!;
        uBuffer.Position(0);
        vBuffer.Position(0);
        var u = new byte[uBuffer.Remaining()];
        var v = new byte[vBuffer.Remaining()];
        uBuffer.Get(u);
        vBuffer.Get(v);

        for (var row = 0; row < height / 2; row++)
        {
            for (var col = 0; col < width / 2; col++)
            {
                var offset = row * uvRowStride + col * uvPixelStride;
                nv21[position++] = offset < v.Length ? v[offset] : (byte)0;
                nv21[position++] = offset < u.Length ? u[offset] : (byte)0;
            }
        }
        return nv21;
    }

    private sealed class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private readonly AndroidLiveCamera _owner;
        public FrameAnalyzer(AndroidLiveCamera owner) => _owner = owner;

        public void Analyze(IImageProxy image) => _owner.OnAnalyze(image);

        public global::Android.Util.Size? DefaultTargetResolution => null;

        public int TargetCoordinateSystem => 0;

        public void UpdateTransform(global::Android.Graphics.Matrix? matrix) { }
    }

    private sealed class CameraLifecycleOwner : Java.Lang.Object, ILifecycleOwner
    {
        private readonly LifecycleRegistry _registry;

        public CameraLifecycleOwner() => _registry = new LifecycleRegistry(this);

        public Lifecycle Lifecycle => _registry;

        public void MarkResumed() => _registry.SetCurrentState(Lifecycle.State.Resumed!);

        public void MarkStopped() => _registry.SetCurrentState(Lifecycle.State.Created!);
    }
}
