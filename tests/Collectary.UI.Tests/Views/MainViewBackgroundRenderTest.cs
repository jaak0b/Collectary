using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Collectary.Presentation.Localization;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class MainViewBackgroundRenderTest
{
    [TearDown]
    public void TearDown() => ThemeService.Instance.ApplyColorTheme("Light");

    [Test]
    public void MainView_PaintsTheThemeBackground_SoSingleViewHostsAreNotTransparent()
    {
        ThemeService.Instance.ApplyColorTheme("Graphite");

        var view = new MainView();
        var window = new Window { Content = view, Width = 300, Height = 300 };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var root = (Panel)view.Content!;
            Assert.That(root.Background, Is.Not.Null,
                "MainView must paint its own background: on single-view hosts (Android) there is no Window "
                + "to provide one, so an unpainted root shows the platform's black window background");

            var actual = ((ISolidColorBrush)root.Background!).Color;
            Assert.That(actual, Is.EqualTo(Color.Parse("#313338")),
                "the root must paint the active theme's BackgroundBrush (Graphite is #313338)");
        }
        finally
        {
            window.Close();
        }
    }
}
