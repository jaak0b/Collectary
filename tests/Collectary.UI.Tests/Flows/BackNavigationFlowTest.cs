using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class BackNavigationFlowTest : FlowTestBase
{
    [Test]
    public async Task SystemBack_OnItemEditor_PersistsItemThroughRealStore()
    {
        var preset = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }]
        };
        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var wentBack = false;
        var vm = MakeItemEditorVm(preset, ef, onSaved: () => wentBack = true);
        SetDisplayName(vm, "Persisted by back");

        var handled = await ((ISystemBackHandler)vm).HandleSystemBackAsync();

        var items = await ItemUseCase.GetItemsForPresetAsync(preset.Id);
        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(wentBack, Is.True);
            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].DisplayName, Is.EqualTo("Persisted by back"));
        });
    }
}
