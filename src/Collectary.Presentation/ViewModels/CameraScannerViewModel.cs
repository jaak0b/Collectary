using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class CameraScannerViewModel : ViewModelBase
{
    private readonly ILiveCamera _camera;
    private readonly IBarcodeImageDecoder _decoder;
    private readonly IDialogService _dialogs;
    private readonly Func<Task<bool>> _requestPermission;
    private readonly Action<BarcodeReadResult?> _onResult;
    private readonly Action _navigateBack;
    private CancellationTokenSource? _cts;
    private bool _started;
    private bool _closed;
    private bool _decoding;

    public CameraScannerViewModel(
        ILiveCamera camera,
        IBarcodeImageDecoder decoder,
        IDialogService dialogs,
        Func<Task<bool>> requestPermission,
        Action<BarcodeReadResult?> onResult,
        Action navigateBack)
    {
        _camera = camera;
        _decoder = decoder;
        _dialogs = dialogs;
        _requestPermission = requestPermission;
        _onResult = onResult;
        _navigateBack = navigateBack;
        foreach (var device in camera.GetDevices())
            Cameras.Add(device);
        SelectedCamera = Cameras.FirstOrDefault();
    }

    public ObservableCollection<CameraDevice> Cameras { get; } = new();

    [ObservableProperty]
    public partial CameraDevice? SelectedCamera { get; set; }

    [ObservableProperty]
    public partial Bitmap? Preview { get; set; }

    public bool CanSwitchCamera => Cameras.Count > 1;

    internal Task FrameProcessing { get; private set; } = Task.CompletedTask;

    public async Task StartAsync()
    {
        if (_closed || _started) return;
        _started = true;
        if (!await _requestPermission())
        {
            await _dialogs.ShowMessageAsync(
                LocalizationService.Instance["Barcode_CameraPermissionDenied"],
                LocalizationService.Instance["Barcode_CameraScanner"]);
            await CloseAsync(null, navigateBack: true);
            return;
        }
        await StartCaptureAsync();
    }

    /// <summary>Called when the scanner is left by outside navigation (e.g. a breadcrumb tap), not by Cancel.</summary>
    public void NotifyClosedExternally() => _ = CloseAsync(null, navigateBack: false);

    partial void OnSelectedCameraChanged(CameraDevice? value)
    {
        if (!_started || _closed) return;
        _ = RestartAsync();
    }

    [RelayCommand]
    private void SwitchCamera()
    {
        if (Cameras.Count < 2 || SelectedCamera is null) return;
        var next = (Cameras.IndexOf(SelectedCamera) + 1) % Cameras.Count;
        SelectedCamera = Cameras[next];
    }

    [RelayCommand]
    private Task CancelAsync() => CloseAsync(null, navigateBack: true);

    private async Task StartCaptureAsync()
    {
        try
        {
            _cts = new CancellationTokenSource();
            await _camera.StartAsync(SelectedCamera?.Id, OnFrame, _cts.Token);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Starting the camera preview failed");
            await _dialogs.ShowMessageAsync(
                LocalizationService.Instance["Barcode_CameraStartFailed"],
                LocalizationService.Instance["Barcode_CameraScanner"]);
            await CloseAsync(null, navigateBack: true);
        }
    }

    private async Task RestartAsync()
    {
        await _camera.StopAsync();
        if (_closed) return;
        _cts?.Dispose();
        await StartCaptureAsync();
    }

    private void OnFrame(CameraFrame frame)
    {
        if (_closed) return;
        UpdatePreview(frame.JpegBytes);
        if (_decoding) return;
        _decoding = true;
        FrameProcessing = DecodeFrameAsync(frame.JpegBytes);
    }

    private async Task DecodeFrameAsync(byte[] jpegBytes)
    {
        try
        {
            var result = await Task.Run(() => _decoder.Decode(jpegBytes));
            if (result is not null && !_closed)
                await CloseAsync(result, navigateBack: true);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Decoding a camera frame failed");
        }
        finally
        {
            _decoding = false;
        }
    }

    private void UpdatePreview(byte[] jpegBytes)
    {
        try
        {
            using var stream = new MemoryStream(jpegBytes);
            var previous = Preview;
            Preview = new Bitmap(stream);
            previous?.Dispose();
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Camera preview frame could not be decoded");
        }
    }

    private async Task CloseAsync(BarcodeReadResult? result, bool navigateBack)
    {
        if (_closed) return;
        _closed = true;
        _cts?.Cancel();
        try
        {
            await _camera.StopAsync();
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Stopping the camera failed");
        }
        _cts?.Dispose();
        _cts = null;
        Preview?.Dispose();
        Preview = null;
        _onResult(result);
        if (navigateBack) _navigateBack();
    }
}
