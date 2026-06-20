using System.Reflection;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class FieldCatalogAttributeTest
{
    private static IEnumerable<Type> ConcreteFieldDefinitions() =>
        typeof(FieldDefinition).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(FieldDefinition).IsAssignableFrom(t));

    [Test]
    public void EveryAddableFieldDefinition_HasFieldCatalogAttribute()
    {
        var missing = ConcreteFieldDefinitions()
            .Where(t => t != typeof(DisplayNameFieldDefinition))
            .Where(t => t.GetCustomAttribute<FieldCatalogAttribute>() is null)
            .Select(t => t.Name)
            .ToList();

        Assert.That(missing, Is.Empty,
            $"These field types must declare [FieldCatalog]: {string.Join(", ", missing)}");
    }

    [Test]
    public void DisplayNameFieldDefinition_HasNoFieldCatalogAttribute() =>
        Assert.That(typeof(DisplayNameFieldDefinition).GetCustomAttribute<FieldCatalogAttribute>(), Is.Null);

    [Test]
    public void Attribute_ExposesOrderAndCategory()
    {
        var attr = typeof(TextFieldDefinition).GetCustomAttribute<FieldCatalogAttribute>()!;
        Assert.Multiple(() =>
        {
            Assert.That(attr.Category, Is.EqualTo(FieldCategory.Text));
            Assert.That(attr.Order, Is.EqualTo(0));
        });
    }

    [Test]
    public void Categories_GroupTypesAsExpected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Category(typeof(ListFieldDefinition)), Is.EqualTo(FieldCategory.Structure));
            Assert.That(Category(typeof(ImageFieldDefinition)), Is.EqualTo(FieldCategory.MediaAndFiles));
            Assert.That(Category(typeof(BoolFieldDefinition)), Is.EqualTo(FieldCategory.Choice));
            Assert.That(Category(typeof(CurrencyFieldDefinition)), Is.EqualTo(FieldCategory.Numbers));
        });
    }

    private static FieldCategory Category(Type t) =>
        t.GetCustomAttribute<FieldCatalogAttribute>()!.Category;
}
