using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class ThemeAccentRenderTest
{
    [TearDown]
    public void TearDown()
    {
        ThemeService.Instance.ApplyCustomColors((IReadOnlyDictionary<string, Color>?)null);
        ThemeService.Instance.ApplyAccent(null);
        ThemeService.Instance.ApplyColorTheme("Light");
    }

    [Test]
    public void CheckedCheckBoxAndSlider_RenderUnderGraphite_WithoutCrashing()
    {
        ThemeService.Instance.ApplyColorTheme("Graphite");

        var panel = new StackPanel();
        panel.Children.Add(new CheckBox { IsChecked = true });
        panel.Children.Add(new Slider { Minimum = 0, Maximum = 10, Value = 5 });

        var window = new Window { Content = panel, Width = 300, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(window.IsVisible, Is.True);
    }
}
