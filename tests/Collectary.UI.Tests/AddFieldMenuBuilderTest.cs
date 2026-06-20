using System.Windows.Input;
using Avalonia.Controls;
using FakeItEasy;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests;

[TestFixture]
public class AddFieldMenuBuilderTest
{
    private static List<Control> Build(IReadOnlyList<FieldTypeCatalogEntry> entries)
    {
        LocalizationService.Instance.Apply("en");
        return new AddFieldMenuBuilder().BuildCatalogItems(entries, A.Fake<ICommand>());
    }

    private static bool IsCategoryHeader(MenuItem item) => item.Classes.Contains("category-header");

    [Test]
    public void EachEntry_RendersIconInIconFont_AndNameAsHeader()
    {
        var entries = new FieldTypeCatalog().Entries;
        var menuItems = Build(entries).OfType<MenuItem>().Where(m => !IsCategoryHeader(m)).ToList();

        Assert.That(menuItems, Has.Count.EqualTo(entries.Count));

        foreach (var entry in entries)
        {
            var item = menuItems.Single(m => ReferenceEquals(m.CommandParameter, entry));
            Assert.That(item.Header, Is.EqualTo(entry.Name),
                "Header must be the plain name so it renders in the UI font, not the icon font.");
            Assert.That(item.Icon, Is.TypeOf<TextBlock>());
            var icon = (TextBlock)item.Icon!;
            Assert.That(icon.Text, Is.EqualTo(entry.Icon));
            Assert.That(icon.Classes, Does.Contain("icon"));
        }
    }

    [Test]
    public void InsertsSeparators_BetweenCategories() =>
        Assert.That(Build(new FieldTypeCatalog().Entries).OfType<Separator>().Any(), Is.True);

    [Test]
    public void RendersDisabledLocalizedHeader_PerCategory()
    {
        var entries = new FieldTypeCatalog().Entries;
        var headers = Build(entries).OfType<MenuItem>().Where(IsCategoryHeader).ToList();
        var categories = entries.Select(e => e.Category).Distinct().ToList();

        Assert.That(headers, Has.Count.EqualTo(categories.Count));
        Assert.That(headers.Select(h => h.Header),
            Is.EquivalentTo(categories.Select(c => LocalizationService.Instance[$"FieldCategory_{c}"])));
        Assert.That(headers.All(h => !h.IsEnabled), Is.True);
    }
}
