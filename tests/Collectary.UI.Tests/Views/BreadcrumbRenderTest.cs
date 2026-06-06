using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Controls;
using Collectary.UI.Views.Helpers;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class BreadcrumbRenderTest
{
    private static BreadcrumbItem Item(string title, bool isHome = false, bool isCurrent = false) =>
        new(title, null, null, isHome, isCurrent);

    [Test]
    public void CurrentCrumbTitle_ShrinksAsTheWindowNarrows()
    {
        var builder = new BreadcrumbVisualBuilder();
        var panel = new BreadcrumbBarPanel { OverflowReservation = 44 };
        panel.Children.Add(builder.BuildCrumb(Item("My Collections", isHome: true)));

        var overflow = new Border { Width = 44, Height = 30 };
        BreadcrumbBarPanel.SetIsOverflow(overflow, true);
        panel.Children.Add(overflow);

        panel.Children.Add(builder.BuildCrumb(Item("A very long current breadcrumb title that must trim", isCurrent: true)));

        var window = new Window { Content = panel, Width = 700, Height = 48 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var currentGrid = panel.Children.OfType<DockPanel>().Last();
        var title = (TextBlock)currentGrid.Children.OfType<Button>().Single().Content!;
        var wide = title.Bounds.Width;

        window.Width = 240;
        Dispatcher.UIThread.RunJobs();
        var narrow = title.Bounds.Width;
        var panelW = panel.Bounds.Width;
        var gridW = currentGrid.Bounds.Width;

        Assert.That(narrow, Is.LessThan(wide),
            $"the current crumb title must keep trimming as the window narrows (wide={wide}, narrow={narrow}, panelW={panelW}, gridW={gridW})");
    }
}
