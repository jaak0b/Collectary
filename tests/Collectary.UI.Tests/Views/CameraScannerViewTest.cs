using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class CameraScannerViewTest
{
    private static ILiveCamera FakeCamera(params CameraDevice[] devices)
    {
        var camera = A.Fake<ILiveCamera>();
        A.CallTo(() => camera.GetDevices()).Returns(devices);
        A.CallTo(() => camera.StartAsync(A<string?>._, A<Action<CameraFrame>>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);
        A.CallTo(() => camera.StopAsync()).Returns(Task.CompletedTask);
        return camera;
    }

    [Test]
    public void CancelButton_NavigatesBackAndDeliversNull()
    {
        var navigated = false;
        BarcodeReadResult? delivered = new("x", BarcodeSymbology.QrCode);
        var resultDelivered = false;
        var vm = new CameraScannerViewModel(FakeCamera(new CameraDevice("0", "Front")),
            A.Fake<IBarcodeImageDecoder>(),
            A.Fake<Collectary.Presentation.Services.IDialogService>(),
            () => Task.FromResult(true),
            r => { resultDelivered = true; delivered = r; },
            () => navigated = true);

        var view = new CameraScannerView { DataContext = vm };
        var window = new Window { Content = view, Width = 600, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var cancel = view.GetVisualDescendants().OfType<Button>()
            .Single(b => ReferenceEquals(b.Command, vm.CancelCommand));
        cancel.Command!.Execute(null);

        Assert.That(resultDelivered, Is.True);
        Assert.That(delivered, Is.Null);
        Assert.That(navigated, Is.True);
    }

    [Test]
    public void Starts_CameraWhenAttached()
    {
        var camera = FakeCamera(new CameraDevice("0", "Front"));
        var vm = new CameraScannerViewModel(camera, A.Fake<IBarcodeImageDecoder>(),
            A.Fake<Collectary.Presentation.Services.IDialogService>(),
            () => Task.FromResult(true), _ => { }, () => { });

        var view = new CameraScannerView { DataContext = vm };
        var window = new Window { Content = view, Width = 600, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        A.CallTo(() => camera.StartAsync(A<string?>._, A<Action<CameraFrame>>._, A<CancellationToken>._))
            .MustHaveHappened();
    }
}
