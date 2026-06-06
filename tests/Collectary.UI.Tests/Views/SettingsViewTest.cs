using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class SettingsViewTest
{
    private static void Render(double width)
    {
        var vm = new SettingsViewModel(navigateToSharedFields: () => { });
        var view = new SettingsView { DataContext = vm };
        var window = new Window { Content = view, Width = width, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        vm.SyncProvider = Core.Domain.CloudProvider.OneDrive;
        Dispatcher.UIThread.RunJobs();
    }

    [Test]
    public void RendersOnNarrowPhoneWidth_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Render(320));
    }

    [Test]
    public void RendersOnWideDesktopWidth_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Render(1000));
    }
}
