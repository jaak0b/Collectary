using Collectary.Core.Domain;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Templates;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class PresetTemplatePickerViewModelTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    private PresetTemplatePickerViewModel CreateSut(
        Action<Preset>? onChosen = null,
        Action? onCancel = null) =>
        new(TemplateTestHelper.Library(),
            onTemplateChosen: onChosen ?? (_ => { }),
            onCancel: onCancel ?? (() => { }));

    [Test]
    public void Constructor_PopulatesCategories()
    {
        var sut = CreateSut();
        Assert.That(sut.Categories, Is.Not.Empty);
        Assert.That(sut.Categories.All(c => c.Templates.Count > 0), Is.True);
    }

    [Test]
    public void Categories_HaveLocalizedHeaders()
    {
        var sut = CreateSut();
        Assert.That(sut.Categories.Any(c => c.Header == "Media & Entertainment"), Is.True);
        Assert.That(sut.Categories.All(c => !c.Header.StartsWith("TemplateCategory_")), Is.True);
    }

    [Test]
    public void Row_ExposesResolvedNameAndIcon()
    {
        var sut = CreateSut();
        var allRows = sut.Categories.SelectMany(c => c.Templates).ToList();
        var books = allRows.Single(r => r.Template.Key == "books");
        Assert.That(books.Name, Is.EqualTo("Books"));
        Assert.That(books.Icon, Is.EqualTo("📚"));
        Assert.That(books.Description, Is.Not.Empty.And.Not.StartsWith("Tmpl_"));
    }

    [Test]
    public void SelectCommand_InvokesOnChosen_WithBuiltPreset()
    {
        Preset? chosen = null;
        var sut = CreateSut(onChosen: p => chosen = p);
        var books = sut.Categories.SelectMany(c => c.Templates).Single(r => r.Template.Key == "books");

        books.SelectCommand.Execute(null);

        Assert.That(chosen, Is.Not.Null);
        Assert.That(chosen!.Name, Is.EqualTo("Books"));
        Assert.That(chosen.Fields, Is.Not.Empty);
    }

    [Test]
    public void SelectCommand_BuildsFreshPresetEachTime()
    {
        var sut = CreateSut(onChosen: _ => { });
        var books = sut.Categories.SelectMany(c => c.Templates).Single(r => r.Template.Key == "books");

        Preset? first = null;
        Preset? second = null;
        var picker = CreateSut(onChosen: p => first = p);
        var row1 = picker.Categories.SelectMany(c => c.Templates).Single(r => r.Template.Key == "books");
        row1.SelectCommand.Execute(null);
        var picker2 = CreateSut(onChosen: p => second = p);
        var row2 = picker2.Categories.SelectMany(c => c.Templates).Single(r => r.Template.Key == "books");
        row2.SelectCommand.Execute(null);

        Assert.That(first!.Id, Is.Not.EqualTo(second!.Id), "each pick must produce an independent Preset instance");
    }

    [Test]
    public void CancelCommand_InvokesOnCancel()
    {
        var cancelled = false;
        var sut = CreateSut(onCancel: () => cancelled = true);

        sut.CancelCommand.Execute(null);

        Assert.That(cancelled, Is.True);
    }
}
