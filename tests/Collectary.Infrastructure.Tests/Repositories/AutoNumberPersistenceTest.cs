using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Tests.Repositories;

[TestFixture]
public class AutoNumberPersistenceTest : DbIntegrationTestBase
{
    [Test]
    public async Task AutoNumberDefinitionAndValue_RoundTrip()
    {
        var preset = new Preset { Name = "Trains" };
        var field = new AutoNumberFieldDefinition
        {
            Label = "Number",
            PresetId = preset.Id,
            Editable = true,
            Strategy = AutoNumberStrategy.FillGaps,
            OnDuplicate = DuplicateHandling.Warn,
            ShowInList = false,
        };
        preset.Fields.Add(field);

        using (var db = DbFactory())
        {
            db.Presets.Add(preset);
            db.SaveChanges();
        }

        var repo = new ItemRepository(DbFactory);
        var item = new Item { PresetId = preset.Id, DisplayName = "BR 01" };
        item.Values.Add(new AutoNumberFieldValue { FieldDefinitionId = field.Id, Value = 7 });
        await repo.AddAsync(item);

        var loaded = await repo.GetByIdAsync(item.Id);
        var value = loaded!.Values.OfType<AutoNumberFieldValue>().Single();

        using var verify = DbFactory();
        var def = (AutoNumberFieldDefinition)verify.Set<FieldDefinition>().Single(f => f.Id == field.Id);

        Assert.Multiple(() =>
        {
            Assert.That(value.Value, Is.EqualTo(7));
            Assert.That(def.Editable, Is.True);
            Assert.That(def.Strategy, Is.EqualTo(AutoNumberStrategy.FillGaps));
            Assert.That(def.OnDuplicate, Is.EqualTo(DuplicateHandling.Warn));
            Assert.That(def.ShowInList, Is.False);
        });
    }

    [Test]
    public async Task GetUsedAutoNumbers_SpansEveryItemForTheField_AndExcludesCurrent()
    {
        var presetA = new Preset { Name = "A" };
        var presetB = new Preset { Name = "B" };
        var field = new AutoNumberFieldDefinition { Label = "Number", PresetId = presetA.Id };
        presetA.Fields.Add(field);

        using (var db = DbFactory())
        {
            db.Presets.AddRange(presetA, presetB);
            db.SaveChanges();
        }

        var repo = new ItemRepository(DbFactory);
        var inA = new Item { PresetId = presetA.Id, DisplayName = "a" };
        inA.Values.Add(new AutoNumberFieldValue { FieldDefinitionId = field.Id, Value = 1 });
        var inB = new Item { PresetId = presetB.Id, DisplayName = "b" };
        inB.Values.Add(new AutoNumberFieldValue { FieldDefinitionId = field.Id, Value = 2 });
        var current = new Item { PresetId = presetA.Id, DisplayName = "current" };
        current.Values.Add(new AutoNumberFieldValue { FieldDefinitionId = field.Id, Value = 9 });
        await repo.AddAsync(inA);
        await repo.AddAsync(inB);
        await repo.AddAsync(current);

        var used = await repo.GetUsedAutoNumbersAsync(field.Id, current.Id);

        Assert.That(used, Is.EquivalentTo(new[] { 1, 2 }));
    }
}
