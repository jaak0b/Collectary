#if DEBUG
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.Templates.Catalog;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.ItemEditor;

[TestFixture]
public class DeveloperTemplateRoundTripTest : FlowTestBase
{
    [Test]
    public async Task DeveloperPreset_FilledRandomly_PersistsGroupsListsAndScalars()
    {
        var preset = new DeveloperTemplate().Build();
        await PresetUseCase.CreatePresetAsync(preset);

        var reloaded = (await PresetRepo.GetAllAsync()).Single(p => p.Id == preset.Id);
        Assert.That(reloaded.Groups, Is.Not.Empty, "group should survive the round-trip");

        var effective = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var ctx = MakeItemContext();
        ctx.SampleData = new BogusSampleData(2024);
        var vm = new ItemEditorViewModel(
            ItemUseCase, PresetUseCase, reloaded, effective,
            onSaved: () => { }, onCancelled: () => { }, context: ctx, existing: null);
        ctx.SaveAsync = vm.PersistAsync;

        vm.FillRandomCommand.Execute(null);
        await vm.PersistAsync();

        var item = (await ItemUseCase.GetItemsForPresetAsync(preset.Id)).Single();
        var full = await ItemRepo.GetByIdAsync(item.Id);

        Assert.That(full, Is.Not.Null);
        Assert.That(full!.DisplayName, Is.Not.Empty);
        Assert.That(full.Values.OfType<TextFieldValue>().Any(v => !v.IsEmpty), Is.True, "scalar text persisted");
        Assert.That(full.Values.OfType<MeasurementFieldValue>().Any(v => !v.IsEmpty), Is.True, "grouped field persisted");

        var list = full.Values.OfType<ListFieldValue>().Single();
        Assert.That(list.Entries, Is.Not.Empty, "list entries persisted");
        Assert.That(list.Entries[0].SubValues.Any(v => !v.IsEmpty), Is.True, "list sub-values persisted");
    }
}
#endif
