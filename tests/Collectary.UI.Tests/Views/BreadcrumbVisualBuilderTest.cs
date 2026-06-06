using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views.Helpers;
using FakeItEasy;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class BreadcrumbVisualBuilderTest
{
    private BreadcrumbVisualBuilder _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new BreadcrumbVisualBuilder();

    private static BreadcrumbItem Item(string title, bool isHome = false, bool isCurrent = false, ICommand? command = null) =>
        new(title, command, null, isHome, isCurrent);

    private static (DockPanel dock, Button button, TextBlock title) Parts(Control crumb)
    {
        var dock = (DockPanel)crumb;
        var button = dock.Children.OfType<Button>().Single();
        return (dock, button, (TextBlock)button.Content!);
    }

    [Test]
    public void BuildCrumb_NonHomeTitle_StretchesSoItTrimsAdaptivelyToTheAvailableWidth()
    {
        var (_, button, title) = Parts(_builder.BuildCrumb(Item("A very long crumb title", isCurrent: true)));

        Assert.That(title.HorizontalAlignment, Is.EqualTo(HorizontalAlignment.Stretch), "the title must fill the clamped width so ellipsis trimming kicks in");
        Assert.That(button.HorizontalContentAlignment, Is.EqualTo(HorizontalAlignment.Stretch), "left content alignment would size the title to its desired width and defeat trimming");
        Assert.That(button.MinWidth, Is.EqualTo(0), "the button must be free to shrink below any theme minimum");
    }

    [Test]
    public void BuildCrumb_NonHomeTitle_CapsWidthAndTrimsWithEllipsis()
    {
        var (_, _, title) = Parts(_builder.BuildCrumb(Item("title")));

        Assert.That(title.MaxWidth, Is.EqualTo(_builder.MaxCrumbWidth));
        Assert.That(title.TextTrimming, Is.EqualTo(TextTrimming.CharacterEllipsis));
    }

    [Test]
    public void BuildCrumb_NonHome_HasLeadingSeparatorDockedLeftAndTitleFills()
    {
        var dock = (DockPanel)_builder.BuildCrumb(Item("Child"));

        var separator = dock.Children.OfType<TextBlock>().Single(t => t.Text == "/");
        Assert.That(DockPanel.GetDock(separator), Is.EqualTo(Dock.Left));
        Assert.That(dock.LastChildFill, Is.True, "the title must fill the remaining space so it trims to the crumb's width");
        Assert.That(dock.Children.Last(), Is.InstanceOf<Button>(), "the title button is the fill child");
    }

    [Test]
    public void BuildCrumb_Home_HasNoSeparator()
    {
        var crumb = _builder.BuildCrumb(Item("My Collections", isHome: true));

        Assert.That(crumb, Is.InstanceOf<Button>());
    }

    [Test]
    public void BuildCrumb_WiresNavigateCommandAndParameter()
    {
        var command = A.Fake<ICommand>();
        var item = new BreadcrumbItem("Child", command, "param", isHome: false, isCurrent: false);

        var (_, button, _) = Parts(_builder.BuildCrumb(item));

        Assert.That(button.Command, Is.SameAs(command));
        Assert.That(button.CommandParameter, Is.EqualTo("param"));
    }
}
