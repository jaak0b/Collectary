using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class CameraScannerViewModelTest
{
    private static void Pump(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }

        if (!task.IsCompleted)
            throw new TimeoutException("Frame processing did not complete while pumping the dispatcher.");
        task.GetAwaiter().GetResult();
    }

    private static byte[] SampleImageBytes()
    {
        var bmp = new RenderTargetBitmap(new PixelSize(2, 2));
        using var ms = new MemoryStream();
        bmp.Save(ms);
        return ms.ToArray();
    }

    private static ILiveCamera FakeCamera(out Action<CameraFrame>[] captured, params CameraDevice[] devices)
    {
        var box = new Action<CameraFrame>[1];
        captured = box;
        var camera = A.Fake<ILiveCamera>();
        A.CallTo(() => camera.GetDevices()).Returns(devices);
        A.CallTo(() => camera.StartAsync(A<string?>._, A<Action<CameraFrame>>._, A<CancellationToken>._))
            .Invokes((string? _, Action<CameraFrame> cb, CancellationToken _) => box[0] = cb)
            .Returns(Task.CompletedTask);
        A.CallTo(() => camera.StopAsync()).Returns(Task.CompletedTask);
        return camera;
    }

    private static CameraScannerViewModel MakeSut(
        ILiveCamera camera,
        IBarcodeImageDecoder decoder,
        Action<BarcodeReadResult?> onResult,
        Action navigateBack,
        IDialogService? dialogs = null,
        Func<Task<bool>>? requestPermission = null) =>
        new(camera, decoder, dialogs ?? A.Fake<IDialogService>(),
            requestPermission ?? (() => Task.FromResult(true)), onResult, navigateBack);

    [Test]
    public void Start_PopulatesCamerasAndSelectsFirst()
    {
        var camera = FakeCamera(out _,
            new CameraDevice("0", "Front"), new CameraDevice("1", "Back"));
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(), _ => { }, () => { });

        Assert.That(sut.Cameras, Has.Count.EqualTo(2));
        Assert.That(sut.SelectedCamera!.Id, Is.EqualTo("0"));
        Assert.That(sut.CanSwitchCamera, Is.True);
    }

    [Test]
    public void Start_WithSingleCamera_CannotSwitch()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(), _ => { }, () => { });

        Assert.That(sut.CanSwitchCamera, Is.False);
    }

    [Test]
    public void Start_WithNoCameras_SelectsNothing()
    {
        var camera = FakeCamera(out _);
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(), _ => { }, () => { });

        Assert.That(sut.SelectedCamera, Is.Null);
        Assert.That(sut.CanSwitchCamera, Is.False);
    }

    [Test]
    public async Task Frame_ThatDecodes_ClosesWithResultStopsAndNavigatesBack()
    {
        var camera = FakeCamera(out var captured, new CameraDevice("0", "Front"));
        var decoder = A.Fake<IBarcodeImageDecoder>();
        var hit = new BarcodeReadResult("9780262033848", BarcodeSymbology.Ean13);
        A.CallTo(() => decoder.Decode(A<byte[]>._)).Returns(hit);

        BarcodeReadResult? result = null;
        var resultCalled = false;
        var navigated = false;
        var sut = MakeSut(camera, decoder,
            r => { resultCalled = true; result = r; }, () => navigated = true);
        await sut.StartAsync();

        captured[0]!(new CameraFrame(SampleImageBytes(), 2, 2));
        Pump(sut.FrameProcessing);

        Assert.That(resultCalled, Is.True);
        Assert.That(result, Is.EqualTo(hit));
        Assert.That(navigated, Is.True);
        A.CallTo(() => camera.StopAsync()).MustHaveHappened();
    }

    [Test]
    public async Task Frame_ThatDecodes_DisposesPreviewAfterClose()
    {
        var camera = FakeCamera(out var captured, new CameraDevice("0", "Front"));
        var decoder = A.Fake<IBarcodeImageDecoder>();
        A.CallTo(() => decoder.Decode(A<byte[]>._))
            .Returns(new BarcodeReadResult("x", BarcodeSymbology.QrCode));
        var sut = MakeSut(camera, decoder, _ => { }, () => { });
        await sut.StartAsync();

        captured[0]!(new CameraFrame(SampleImageBytes(), 2, 2));
        Pump(sut.FrameProcessing);

        Assert.That(sut.Preview, Is.Null);
    }

    [Test]
    public async Task Frame_ThatDoesNotDecode_KeepsScanning()
    {
        var camera = FakeCamera(out var captured, new CameraDevice("0", "Front"));
        var decoder = A.Fake<IBarcodeImageDecoder>();
        A.CallTo(() => decoder.Decode(A<byte[]>._)).Returns(null);

        var resultCalled = false;
        var sut = MakeSut(camera, decoder, _ => resultCalled = true, () => { });
        await sut.StartAsync();

        captured[0]!(new CameraFrame(SampleImageBytes(), 2, 2));
        Pump(sut.FrameProcessing);

        Assert.That(resultCalled, Is.False);
        Assert.That(sut.Preview, Is.Not.Null);
    }

    [Test]
    public async Task SecondFrame_AfterFirstProcessed_StillDecodesAndCloses()
    {
        var camera = FakeCamera(out var captured, new CameraDevice("0", "Front"));
        var decoder = A.Fake<IBarcodeImageDecoder>();
        var hit = new BarcodeReadResult("9780262033848", BarcodeSymbology.Ean13);
        A.CallTo(() => decoder.Decode(A<byte[]>._)).ReturnsNextFromSequence(null, hit);
        var resultCalled = false;
        var sut = MakeSut(camera, decoder, _ => resultCalled = true, () => { });
        await sut.StartAsync();

        captured[0]!(new CameraFrame(SampleImageBytes(), 2, 2));
        Pump(sut.FrameProcessing);
        captured[0]!(new CameraFrame(SampleImageBytes(), 2, 2));
        Pump(sut.FrameProcessing);

        Assert.That(resultCalled, Is.True);
    }

    [Test]
    public async Task Frame_WhenDecoderThrows_KeepsScanningWithoutCrashing()
    {
        var camera = FakeCamera(out var captured, new CameraDevice("0", "Front"));
        var decoder = A.Fake<IBarcodeImageDecoder>();
        A.CallTo(() => decoder.Decode(A<byte[]>._)).Throws(new InvalidOperationException("bad frame"));
        var resultCalled = false;
        var sut = MakeSut(camera, decoder, _ => resultCalled = true, () => { });
        await sut.StartAsync();

        captured[0]!(new CameraFrame(SampleImageBytes(), 2, 2));
        Pump(sut.FrameProcessing);

        Assert.That(resultCalled, Is.False);
    }

    [Test]
    public async Task Cancel_WhenStopThrows_StillDeliversNullAndNavigates()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        A.CallTo(() => camera.StopAsync()).Throws(new InvalidOperationException("stop failed"));
        var resultCalled = false;
        var navigated = false;
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(),
            _ => resultCalled = true, () => navigated = true);
        await sut.StartAsync();

        await sut.CancelCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(resultCalled, Is.True);
            Assert.That(navigated, Is.True);
        });
    }

    [Test]
    public async Task StartAsync_WhenCameraPermissionDenied_ShowsDialogAndClosesWithoutStarting()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        var dialogs = A.Fake<IDialogService>();
        BarcodeReadResult? result = new("x", BarcodeSymbology.QrCode);
        var resultCalled = false;
        var navigated = false;
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(),
            r => { resultCalled = true; result = r; }, () => navigated = true,
            dialogs, requestPermission: () => Task.FromResult(false));

        await sut.StartAsync();

        A.CallTo(() => camera.StartAsync(A<string?>._, A<Action<CameraFrame>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => dialogs.ShowMessageAsync(A<string>._, A<string>._)).MustHaveHappened();
        Assert.Multiple(() =>
        {
            Assert.That(resultCalled, Is.True);
            Assert.That(result, Is.Null);
            Assert.That(navigated, Is.True);
        });
    }

    [Test]
    public async Task StartAsync_WhenPermissionGranted_StartsTheCamera()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(), _ => { }, () => { },
            requestPermission: () => Task.FromResult(true));

        await sut.StartAsync();

        A.CallTo(() => camera.StartAsync("0", A<Action<CameraFrame>>._, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Test]
    public async Task StartAsync_CalledTwice_DoesNotDoubleStartTheCamera()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(), _ => { }, () => { });

        await sut.StartAsync();
        await sut.StartAsync();

        A.CallTo(() => camera.StartAsync(A<string?>._, A<Action<CameraFrame>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task StartAsync_WhenCameraThrows_SurfacesDialogAndCloses()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        A.CallTo(() => camera.StartAsync(A<string?>._, A<Action<CameraFrame>>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("device busy"));
        var dialogs = A.Fake<IDialogService>();

        BarcodeReadResult? result = new("x", BarcodeSymbology.QrCode);
        var resultCalled = false;
        var navigated = false;
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(),
            r => { resultCalled = true; result = r; }, () => navigated = true, dialogs);

        await sut.StartAsync();

        A.CallTo(() => dialogs.ShowMessageAsync(A<string>._, A<string>._)).MustHaveHappened();
        Assert.That(resultCalled, Is.True);
        Assert.That(result, Is.Null);
        Assert.That(navigated, Is.True);
    }

    [Test]
    public async Task Cancel_ClosesWithNullAndNavigatesBack()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        BarcodeReadResult? result = new("x", BarcodeSymbology.QrCode);
        var resultCalled = false;
        var navigated = false;
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(),
            r => { resultCalled = true; result = r; }, () => navigated = true);
        await sut.StartAsync();

        await sut.CancelCommand.ExecuteAsync(null);

        Assert.That(resultCalled, Is.True);
        Assert.That(result, Is.Null);
        Assert.That(navigated, Is.True);
        A.CallTo(() => camera.StopAsync()).MustHaveHappened();
    }

    [Test]
    public async Task NotifyClosedExternally_CompletesWithNullWithoutNavigating()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        var resultCalled = false;
        var navigated = false;
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(),
            _ => resultCalled = true, () => navigated = true);
        await sut.StartAsync();

        sut.NotifyClosedExternally();

        Assert.That(resultCalled, Is.True);
        Assert.That(navigated, Is.False);
        A.CallTo(() => camera.StopAsync()).MustHaveHappened();
    }

    [Test]
    public async Task NotifyClosedExternally_AfterResult_DoesNotDeliverTwice()
    {
        var camera = FakeCamera(out _, new CameraDevice("0", "Front"));
        var resultCount = 0;
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(),
            _ => resultCount++, () => { });
        await sut.StartAsync();

        await sut.CancelCommand.ExecuteAsync(null);
        sut.NotifyClosedExternally();

        Assert.That(resultCount, Is.EqualTo(1));
    }

    [Test]
    public async Task SwitchCamera_AdvancesSelectionAndRestartsOnNewDevice()
    {
        var camera = FakeCamera(out _,
            new CameraDevice("0", "Front"), new CameraDevice("1", "Back"));
        var sut = MakeSut(camera, A.Fake<IBarcodeImageDecoder>(), _ => { }, () => { });
        await sut.StartAsync();

        sut.SwitchCameraCommand.Execute(null);

        Assert.That(sut.SelectedCamera!.Id, Is.EqualTo("1"));
        A.CallTo(() => camera.StopAsync()).MustHaveHappened();
        A.CallTo(() => camera.StartAsync("1", A<Action<CameraFrame>>._, A<CancellationToken>._))
            .MustHaveHappened();
    }
}
