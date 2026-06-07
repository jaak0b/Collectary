using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class SettingsViewTest
{
    private string _dir = null!;
    private string _original = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _original = AppPreferences.FilePath;
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _original;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

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

    [Test]
    public void CustomThemeBadge_VisibleOnlyWhenCustomized()
    {
        var vm = new SettingsViewModel(navigateToSharedFields: () => { });
        var view = new SettingsView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var badge = view.FindControl<TextBlock>("CustomThemeBadge")!;
        Assert.That(badge.IsVisible, Is.False, "the badge is hidden while the theme is unmodified");

        vm.ColorSlots.First(s => s.Key == "Background").Color = Colors.Magenta;
        Dispatcher.UIThread.RunJobs();

        Assert.Multiple(() =>
        {
            Assert.That(badge.IsVisible, Is.True, "the badge appears once the theme is customized");
            Assert.That(badge.Text, Does.Contain("Light"));
        });
    }
}
