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
    public void LabelColumn_IsSharedAndCapped_SoLongLabelsWrapInsteadOfWideningTheColumn()
    {
        var theme = (ControlTheme)((ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Collectary.UI/Controls/SettingsRow.axaml")))[typeof(SettingsRow)]!;

        var shortRow = new SettingsRow { Theme = theme, Label = "A", Content = new TextBlock { Text = "x" } };
        var longRow = new SettingsRow
        {
            Theme = theme,
            Label = "Automatische Synchronisierung Sehr Lang",
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

            Assert.That(Content(shortRow).Bounds.X, Is.EqualTo(Content(longRow).Bounds.X).Within(0.5),
                "every row's control must line up at the same left edge (one shared label column)");

            Assert.That(Content(shortRow).Bounds.X, Is.LessThan(200),
                "the shared column must stay capped: a very long label must wrap rather than push the "
                + "column out to its full width and leave a giant gap before short labels' controls");

            Assert.That(Label(longRow).Bounds.Height, Is.GreaterThan(Label(shortRow).Bounds.Height * 1.4),
                "a label longer than the cap must wrap onto multiple lines (on spaces)");
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void NarrowStackedLayout_LabelIsLeftAligned_NotCentered()
    {
        var theme = (ControlTheme)((ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Collectary.UI/Controls/SettingsRow.axaml")))[typeof(SettingsRow)]!;

        var row = new SettingsRow
        {
            Theme = theme,
            Label = "Style",
            Content = new TextBlock { Text = "x" },
            NarrowThreshold = 400,
        };
        var window = new Window { Content = row, Width = 300, Height = 200 };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.That(row.Classes.Contains(":narrow"), Is.True, "precondition: row is in the stacked layout");
            Assert.That(Label(row).Bounds.X, Is.LessThan(10),
                "in the stacked (narrow) layout the label must stay left-aligned, not be centered by its "
                + "MaxWidth cap");
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
