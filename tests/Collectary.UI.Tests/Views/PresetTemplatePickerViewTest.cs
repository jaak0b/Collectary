using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Templates;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class PresetTemplatePickerViewTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    private static PresetTemplatePickerViewModel CreateViewModel() =>
        new(TemplateTestHelper.Library(), onTemplateChosen: _ => { }, onCancel: () => { });

    private static ScrollViewer FindScroller(PresetTemplatePickerView view) =>
        view.GetVisualDescendants().OfType<ScrollViewer>().First();

    [Test]
    public void LastTemplate_IsFullyVisibleAfterScrollingToEnd()
    {
        var vm = CreateViewModel();
        var view = new PresetTemplatePickerView { DataContext = vm };
        var window = new Window { Content = view, Width = 900, Height = 380 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var scroller = FindScroller(view);
        Assert.That(scroller.Extent.Height, Is.GreaterThan(scroller.Viewport.Height),
            "the templates must overflow the window for this test to be meaningful");

        scroller.ScrollToEnd();
        Dispatcher.UIThread.RunJobs();

        var lastTemplate = scroller.GetVisualDescendants().OfType<Button>()
            .Select(b => new { Button = b, Bottom = b.TranslatePoint(new Point(0, b.Bounds.Height), scroller) })
            .Where(x => x.Bottom is not null)
            .OrderByDescending(x => x.Bottom!.Value.Y)
            .First();

        Assert.That(lastTemplate.Bottom!.Value.Y, Is.LessThanOrEqualTo(scroller.Bounds.Height + 0.5),
            "after scrolling to the end the last template card must sit fully inside the viewport, " +
            "not be clipped behind the footer");
    }
}
