using Autofac;
using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;
using Collectary.UI.Views;
using FakeItEasy;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class SyncFlyoutProbeTest
{
    private ISyncService _syncService = null!;
    private Window _window = null!;
    private MainView _view = null!;
    private IContainer _container = null!;

    [SetUp]
    public void SetUp()
    {
        _syncService = A.Fake<ISyncService>();
        A.CallTo(() => _syncService.SyncAsync()).Returns(new SyncResult(2, 3));
        var status = A.Fake<ISyncStatus>();
        A.CallTo(() => status.IsConfigured).Returns(true);

        var builder = new ContainerBuilder();
        builder.RegisterInstance(_syncService);
        builder.RegisterInstance(status);
        builder.RegisterInstance<IUiDispatcher>(new InlineUiDispatcher());
        builder.RegisterInstance<IBackgroundRunner>(new InlineBackgroundRunner());
        _container = builder.Build();

        var vm = new MainWindowViewModel(
            _container,
            A.Fake<IPresetUseCase>(), A.Fake<IItemUseCase>(), A.Fake<ISharedFieldUseCase>(),
            A.Fake<IListCellBuilder>(), A.Fake<IFieldEditorRegistry>(), A.Fake<IImageStore>(),
            A.Fake<IDialogService>(), A.Fake<ISyncScheduler>());

        _view = new MainView { DataContext = vm };
        _window = new Window { Content = _view };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    [TearDown]
    public void TearDown()
    {
        _window.Close();
        _container.Dispose();
    }

    private Button StatusButton()
    {
        var button = _view.FindControl<Button>("SyncStatusButton");
        Assert.That(button, Is.Not.Null, "the sync status button must exist in MainView");
        Assert.That(button!.Flyout, Is.Not.Null, "the flyout must be attached to the button");
        return button;
    }

    private StackPanel FlyoutContent(Button button)
    {
        button.Flyout!.ShowAt(button);
        Dispatcher.UIThread.RunJobs();
        return (StackPanel)((Flyout)button.Flyout).Content!;
    }

    [Test]
    public void SyncNowButtonInsideTheFlyout_ExecutesTheSyncCommand()
    {
        var content = FlyoutContent(StatusButton());
        var syncNow = content.Children.OfType<Button>().Single();

        Assert.That(syncNow.Command, Is.Not.Null, "the Sync Now button must carry the sync command");
        syncNow.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        A.CallTo(() => _syncService.SyncAsync()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void AfterSyncing_TheFlyoutShowsThePushedAndPulledCounts()
    {
        var content = FlyoutContent(StatusButton());
        var syncNow = content.Children.OfType<Button>().Single();

        syncNow.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var texts = content.Children.OfType<TextBlock>().Where(t => t.IsVisible).Select(t => t.Text).ToList();
        Assert.That(texts, Has.Some.Contains("2").And.Some.Contains("3"),
            "the flyout must report how many records were pushed and pulled");
    }

    [Test]
    public void ALongNoticeText_WrapsInsteadOfWideningTheFlyoutPastTheScreen()
    {
        A.CallTo(() => _syncService.SyncAsync())
            .Returns(new SyncResult(0, 0, UnreadableDevices: 1));
        var content = FlyoutContent(StatusButton());
        var syncNow = content.Children.OfType<Button>().Single();

        syncNow.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.That(content.Bounds.Width, Is.LessThanOrEqualTo(360),
            "a long notice must wrap inside a capped flyout width, not stretch the popup off-screen");
    }

    [Test]
    public void AfterSyncing_TheLastSyncTimeIsShownInsteadOfNever()
    {
        var content = FlyoutContent(StatusButton());
        var syncNow = content.Children.OfType<Button>().Single();
        var before = content.Children.OfType<TextBlock>().First().Text;

        syncNow.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var after = content.Children.OfType<TextBlock>().First().Text;
        Assert.That(after, Is.Not.EqualTo(before), "the last-sync line must update right inside the open flyout");
    }
}
