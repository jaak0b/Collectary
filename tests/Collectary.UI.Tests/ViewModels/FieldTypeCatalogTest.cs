using System.Reflection;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FieldTypeCatalogTest
{
    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private static int ReflectedAddableCount() =>
        typeof(FieldDefinition).Assembly.GetTypes()
            .Count(t => !t.IsAbstract && !t.IsGenericTypeDefinition
                        && typeof(FieldDefinition).IsAssignableFrom(t)
                        && t.GetCustomAttribute<FieldCatalogAttribute>() is not null);

    [Test]
    public void Entries_ExcludeDisplayName()
    {
        var catalog = new FieldTypeCatalog();
        Assert.That(catalog.Entries.Select(e => e.Type), Has.None.EqualTo(typeof(DisplayNameFieldDefinition)));
    }

    [Test]
    public void Entries_CountMatchesReflectedAddableTypes()
    {
        var catalog = new FieldTypeCatalog();
        Assert.That(catalog.Entries, Has.Count.EqualTo(ReflectedAddableCount()));
    }

    [Test]
    public void Entries_OrderedByCategoryThenOrder()
    {
        var catalog = new FieldTypeCatalog();
        var keys = catalog.Entries.Select(e => ((int)e.Category, e.Order)).ToList();
        Assert.That(keys, Is.Ordered);
        Assert.Multiple(() =>
        {
            Assert.That(catalog.Entries.First().Type, Is.EqualTo(typeof(TextFieldDefinition)));
            Assert.That(catalog.Entries.Last().Type, Is.EqualTo(typeof(ListFieldDefinition)));
        });
    }

    [Test]
    public void Entries_AllHaveNameAndIcon()
    {
        var catalog = new FieldTypeCatalog();
        Assert.That(catalog.Entries.All(e => !string.IsNullOrWhiteSpace(e.Name) && !string.IsNullOrWhiteSpace(e.Icon)), Is.True);
    }

    [Test]
    public void Entries_IncludePreviouslyMissingSharedFieldTypes()
    {
        var catalog = new FieldTypeCatalog();
        var types = catalog.Entries.Select(e => e.Type).ToList();
        Assert.That(types, Is.SupersetOf(new[]
        {
            typeof(RichTextFieldDefinition), typeof(PercentageFieldDefinition), typeof(CurrencyFieldDefinition),
            typeof(TimeFieldDefinition), typeof(DurationFieldDefinition), typeof(PhoneFieldDefinition),
            typeof(EmailFieldDefinition), typeof(TagsFieldDefinition),
        }));
    }

    [Test]
    public void Entries_DoNotIncludeRemovedTypes()
    {
        var typeNames = new FieldTypeCatalog().Entries.Select(e => e.Type.Name).ToList();

        Assert.That(typeNames, Has.None.EqualTo("ProgressFieldDefinition"));
        Assert.That(typeNames, Has.None.EqualTo("SliderFieldDefinition"));
    }

    [Test]
    public void Entries_GroupFieldTypesByDataShape()
    {
        var byType = new FieldTypeCatalog().Entries.ToDictionary(e => e.Type, e => e.Category);

        Assert.Multiple(() =>
        {
            Assert.That(byType[typeof(TextFieldDefinition)], Is.EqualTo(FieldCategory.Text));
            Assert.That(byType[typeof(UrlFieldDefinition)], Is.EqualTo(FieldCategory.Text));
            Assert.That(byType[typeof(IntegerFieldDefinition)], Is.EqualTo(FieldCategory.Numbers));
            Assert.That(byType[typeof(RatingFieldDefinition)], Is.EqualTo(FieldCategory.Numbers));
            Assert.That(byType[typeof(DateFieldDefinition)], Is.EqualTo(FieldCategory.DateTime));
            Assert.That(byType[typeof(DurationFieldDefinition)], Is.EqualTo(FieldCategory.DateTime));
            Assert.That(byType[typeof(TagsFieldDefinition)], Is.EqualTo(FieldCategory.Choice));
            Assert.That(byType[typeof(LinkedItemFieldDefinition)], Is.EqualTo(FieldCategory.Choice));
            Assert.That(byType[typeof(ImageFieldDefinition)], Is.EqualTo(FieldCategory.MediaAndFiles));
            Assert.That(byType[typeof(BarcodeFieldDefinition)], Is.EqualTo(FieldCategory.MediaAndFiles));
            Assert.That(byType[typeof(FileAttachmentFieldDefinition)], Is.EqualTo(FieldCategory.MediaAndFiles));
            Assert.That(byType[typeof(ColorFieldDefinition)], Is.EqualTo(FieldCategory.MediaAndFiles));
            Assert.That(byType[typeof(ListFieldDefinition)], Is.EqualTo(FieldCategory.Structure));
        });
    }

    [Test]
    public void Entries_PlaceAutoNumberWithTheOtherWholeNumbers()
    {
        var types = new FieldTypeCatalog().Entries.Select(e => e.Type).ToList();

        Assert.That(types.IndexOf(typeof(AutoNumberFieldDefinition)),
            Is.LessThan(types.IndexOf(typeof(PercentageFieldDefinition))));
    }

    [Test]
    public void Entries_PlaceImageGalleryImmediatelyAfterImage()
    {
        var types = new FieldTypeCatalog().Entries.Select(e => e.Type).ToList();
        var imageIndex = types.IndexOf(typeof(ImageFieldDefinition));

        Assert.That(types[imageIndex + 1], Is.EqualTo(typeof(MultiImageFieldDefinition)));
    }

    [Test]
    public void Create_ReturnsMatchingRuntimeTypeWithLabel()
    {
        var entry = new FieldTypeCatalog().Entries.First(e => e.Type == typeof(CurrencyFieldDefinition));

        var def = entry.Create();

        Assert.Multiple(() =>
        {
            Assert.That(def, Is.TypeOf<CurrencyFieldDefinition>());
            Assert.That(def.Label, Is.Not.Empty);
        });
    }

    [Test]
    public void Name_ReLocalizesWithLanguage()
    {
        var entry = new FieldTypeCatalog().Entries.First(e => e.Type == typeof(CurrencyFieldDefinition));

        LocalizationService.Instance.Apply("en");
        var en = entry.Name;
        LocalizationService.Instance.Apply("de");
        var de = entry.Name;

        Assert.Multiple(() =>
        {
            Assert.That(en, Is.EqualTo("Currency"));
            Assert.That(de, Is.EqualTo("Währung"));
        });
    }
}
