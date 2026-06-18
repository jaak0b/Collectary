using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

    [Test]
    public void WideLayout_ExpandsLabelColumnToFitLongText_AndKeepsRowsAligned()
    {
        var theme = (ControlTheme)((ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Collectary.UI/Controls/SettingsRow.axaml")))[typeof(SettingsRow)]!;

        var shortRow = new SettingsRow { Theme = theme, Label = "A", Content = new TextBlock { Text = "x" } };
        var longRow = new SettingsRow
        {
            Theme = theme,
            Label = "Beschriftungsposition Sehr Langer Text",
            Content = new TextBlock { Text = "y" },
        };

        var panel = new StackPanel { Width = 600 };
        Grid.SetIsSharedSizeScope(panel, true);
        panel.Children.Add(shortRow);
        panel.Children.Add(longRow);
        var window = new Window { Content = panel, Width = 700, Height = 200 };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.That(Label(longRow).Bounds.Height, Is.LessThan(Label(shortRow).Bounds.Height * 1.6),
                "the label column must widen to fit a long label on one line instead of wrapping it");

            Assert.That(Content(longRow).Bounds.X, Is.EqualTo(Content(shortRow).Bounds.X).Within(0.5),
                "a shared label column must keep every row's control aligned to the same left edge");
        }
        finally
        {
            window.Close();
        }
    }

    private static TextBlock Label(SettingsRow row) =>
        row.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Label");

    private static ContentPresenter Content(SettingsRow row) =>
        row.GetVisualDescendants().OfType<ContentPresenter>().Single(c => c.Name == "PART_Content");
}
