using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class ThemePaletteSwapRenderTest
{
    [TearDown]
    public void TearDown()
    {
        ThemeService.Instance.ApplyCustomColors((IReadOnlyDictionary<string, Color>?)null);
        ThemeService.Instance.ApplyAccent(null);
        ThemeService.Instance.ApplyColorTheme("Light");
    }

    [Test]
    public void SwitchingColorTheme_RepaintsDynamicResourceConsumerInLiveTree()
    {
        var app = Application.Current!;
        var merged = app.Resources.MergedDictionaries;
        var snapshot = merged.ToList();
        try
        {
            merged.Clear();
            var inlinedLight = new ResourceDictionary
            {
                ["BackgroundColor"] = Color.Parse("#FFFFFF"),
                ["BackgroundBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF")),
            };
            merged.Add(inlinedLight);

            var probe = new Border();
            probe.Bind(Border.BackgroundProperty, probe.GetResourceObservable("BackgroundBrush"));
            var window = new Window { Content = probe, Width = 200, Height = 200 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var lightColor = ((ISolidColorBrush)probe.Background!).Color;

            ThemeService.Instance.ApplyColorTheme("Dark");
            Dispatcher.UIThread.RunJobs();

            var darkColor = ((ISolidColorBrush)probe.Background!).Color;

            Assert.Multiple(() =>
            {
                Assert.That(lightColor, Is.Not.EqualTo(Color.Parse("#121212")),
                    "sanity: the Light palette is in effect before the swap");
                Assert.That(darkColor, Is.EqualTo(Color.Parse("#121212")),
                    "a DynamicResource-bound control must repaint when the colour palette is swapped");
            });
        }
        finally
        {
            merged.Clear();
            foreach (var d in snapshot) merged.Add(d);
            ThemeService.Instance.ApplyColorTheme("Light");
        }
    }
}
