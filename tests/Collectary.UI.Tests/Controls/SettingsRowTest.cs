using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class SettingsRowTest
{
    private static SettingsRow Render(double windowWidth)
    {
        var row = new SettingsRow { Label = "Label", Content = new TextBlock { Text = "value" }, NarrowThreshold = 400 };
        var window = new Window { Content = row, Width = windowWidth, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return row;
    }

    [Test]
    public void NarrowWidth_SetsNarrowPseudoClass()
    {
        Assert.That(Render(320).Classes.Contains(":narrow"), Is.True);
    }

    [Test]
    public void WideWidth_DoesNotSetNarrowPseudoClass()
    {
        Assert.That(Render(700).Classes.Contains(":narrow"), Is.False);
    }

    [Test]
    public void Threshold_IsRespected()
    {
        var row = new SettingsRow { NarrowThreshold = 500, Content = new TextBlock() };
        var window = new Window { Content = row, Width = 450, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(row.Classes.Contains(":narrow"), Is.True);
    }
}
