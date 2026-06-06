using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Templates;

namespace Collectary.UI.Tests.Templates;

[TestFixture]
public class PresetTemplateBaseGroupTest
{
    private sealed class Probe : PresetTemplateBase
    {
        public Preset BuildWithGroup()
        {
            var grouped = Text("FieldType_Text");
            var group = Group("Tmpl_Developer_Group", 1, grouped);
            return Compose("Tmpl_Developer_Name", 1, new FieldDefinition[] { grouped }, new[] { group });
        }
    }

    [Test]
    public void Compose_WithGroups_WiresGroupsAndMembership()
    {
        var preset = new Probe().BuildWithGroup();

        Assert.That(preset.Groups, Has.Count.EqualTo(1));
        var group = preset.Groups[0];
        var grouped = preset.Fields.OfType<TextFieldDefinition>().Single();

        Assert.That(grouped.GroupId, Is.EqualTo(group.Id));
        Assert.That(group.PresetId, Is.EqualTo(preset.Id));
    }
}
