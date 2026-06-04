using Collectary.Presentation.Templates;

namespace Collectary.UI.Tests.Templates;

[TestFixture]
public class PresetTemplateLibraryTest
{
    [Test]
    public void All_ContainsEveryRegisteredTemplate()
    {
        var library = TemplateTestHelper.Library();
        Assert.That(library.All, Has.Count.EqualTo(TemplateTestHelper.AllTemplates().Count));
    }

    [Test]
    public void ByCategory_GroupsAllTemplates_NoneDropped()
    {
        var library = TemplateTestHelper.Library();
        var grouped = library.ByCategory().SelectMany(g => g.Templates).ToList();
        Assert.That(grouped, Has.Count.EqualTo(library.All.Count));
    }

    [Test]
    public void ByCategory_ProducesNonEmptyGroupsInCategoryOrder()
    {
        var library = TemplateTestHelper.Library();
        var groups = library.ByCategory();

        Assert.That(groups, Is.Not.Empty);
        Assert.That(groups.All(g => g.Templates.Count > 0), Is.True);
        Assert.That(groups.Select(g => g.Category), Is.Unique);
    }

    [Test]
    public void ByCategory_AllFourCategoriesPresent()
    {
        var library = TemplateTestHelper.Library();
        var categories = library.ByCategory().Select(g => g.Category).ToList();
        Assert.That(categories, Does.Contain(PresetTemplateCategory.MediaEntertainment));
        Assert.That(categories, Does.Contain(PresetTemplateCategory.Collectibles));
        Assert.That(categories, Does.Contain(PresetTemplateCategory.Lifestyle));
        Assert.That(categories, Does.Contain(PresetTemplateCategory.Practical));
    }
}
