using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class FieldDetailEditorTest
{
    [Test]
    public void NarrowOverflowingContent_KeepsControlsClearOfTheScrollbar()
    {
        var vm = new FieldDefinitionRowViewModel(new ListFieldDefinition());
        var view = new FieldDetailEditor { DataContext = vm };
        var window = new Window { Content = view, Width = 280, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var scroller = view.GetVisualDescendants().OfType<ScrollViewer>().First();
        var content = (Control)scroller.Content!;

        Assert.That(content.Bounds.Width, Is.LessThanOrEqualTo(scroller.Bounds.Width - 10),
            "the scrollable content must be inset from the viewport's right edge so the overlay scrollbar does not cover the controls");
    }
}
