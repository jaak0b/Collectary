using FakeItEasy;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class BreadcrumbNodeTest
{
    [Test]
    public void ExposesTitleAndContent()
    {
        var content = A.Fake<ViewModelBase>();
        var node = new BreadcrumbNode("Home", content);
        Assert.That(node.Title, Is.EqualTo("Home"));
        Assert.That(node.Content, Is.SameAs(content));
    }
}
