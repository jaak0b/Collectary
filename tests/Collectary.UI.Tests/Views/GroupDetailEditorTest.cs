using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class GroupDetailEditorTest
{
    [Test]
    public void NarrowContent_KeepsControlsClearOfTheScrollbar()
    {
        var vm = new FieldGroupRowViewModel("Test Group");
        var view = new GroupDetailEditor { DataContext = vm };
        var window = new Window { Content = view, Width = 280, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var scroller = view.GetVisualDescendants().OfType<ScrollViewer>().First();
        var content = (Control)scroller.Content!;

        Assert.That(content.Bounds.Width, Is.LessThanOrEqualTo(scroller.Bounds.Width - 10),
            "the scrollable content must be inset from the viewport's right edge so the overlay scrollbar does not cover the controls");
    }
}
