using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Templates;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.Templates;

#if DEBUG
[TestFixture]
public class DeveloperTemplateTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    private static Core.Domain.Preset Build() =>
        TemplateTestHelper.AllTemplates().Single(t => t.Key == "developer").Build();

    [Test]
    public void Template_IsDiscoveredByLibrary()
    {
        Assert.That(TemplateTestHelper.AllTemplates().Any(t => t.Key == "developer"), Is.True);
    }

    [Test]
    public void Build_CoversEveryCatalogFieldType()
    {
        var preset = Build();
        var present = preset.Fields.Select(f => f.GetType()).ToHashSet();

        var catalogTypes = new FieldTypeCatalog().Entries.Select(e => e.Type);

        Assert.That(catalogTypes, Is.SubsetOf(present));
    }

    [Test]
    public void Build_HasAListWithSubFields()
    {
        var preset = Build();
        var list = preset.Fields.OfType<ListFieldDefinition>().Single();
        Assert.That(list.SubFields, Is.Not.Empty);
        Assert.That(list.SubFields.All(s => s.ParentListFieldDefinitionId == list.Id), Is.True);
    }

    [Test]
    public void Build_HasAGroupWithMemberFields()
    {
        var preset = Build();

        Assert.That(preset.Groups, Is.Not.Empty);
        var group = preset.Groups[0];
        Assert.That(group.PresetId, Is.EqualTo(preset.Id));
        Assert.That(preset.Fields.Any(f => f.GroupId == group.Id), Is.True);
    }
}
#endif
