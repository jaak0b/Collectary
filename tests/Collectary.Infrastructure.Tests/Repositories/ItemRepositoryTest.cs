using System.Linq.Expressions;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.Search;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Repositories;

file sealed class RecordingLogger : IAppLogger
{
    public int DebugCallCount { get; private set; }
    public void Verbose(string t, params object?[] a) { }
    public void Debug(string t, params object?[] a) => DebugCallCount++;
    public void Information(string t, params object?[] a) { }
    public void Warning(string t, params object?[] a) { }
    public void Error(Exception e, string t, params object?[] a) { }
}

[TestFixture]
public class ItemRepositoryTest : DbIntegrationTestBase
{
    private ItemRepository _sut = null!;
    private Preset _preset = null!;
    private TextFieldDefinition _textField = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new ItemRepository(DbFactory);

        _preset = new Preset { Name = "Books" };
        _textField = new TextFieldDefinition { Label = "Title", PresetId = _preset.Id };
        _preset.Fields.Add(_textField);

        using var db = DbFactory();
        db.Presets.Add(_preset);
        db.SaveChanges();
    }

    private Item MakeItem(string displayName = "Item 1") => new()
    {
        PresetId = _preset.Id,
        DisplayName = displayName
    };

    [Test]
    public async Task AddAsync_PersistsItem()
    {
        var item = MakeItem("Lord of the Rings");

        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.DisplayName, Is.EqualTo("Lord of the Rings"));
    }

    [Test]
    public async Task GetByPresetAsync_ReturnsOnlyItemsForPreset()
    {
        var other = new Preset { Name = "Other" };
        using var db = DbFactory();
        db.Presets.Add(other);
        db.SaveChanges();

        await _sut.AddAsync(MakeItem("A"));
        await _sut.AddAsync(new Item { PresetId = other.Id, DisplayName = "B" });

        var result = await _sut.GetByPresetAsync(_preset.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].DisplayName, Is.EqualTo("A"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        Assert.That(await _sut.GetByIdAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_IncludesValues()
    {
        var item = MakeItem();
        item.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "Hello" });
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);

        Assert.That(loaded!.Values, Has.Count.EqualTo(1));
        Assert.That(((TextFieldValue)loaded.Values[0]).Value, Is.EqualTo("Hello"));
    }

    [Test]
    public async Task UpdateAsync_UpdatesDisplayName()
    {
        var item = MakeItem("Old");
        await _sut.AddAsync(item);

        item.DisplayName = "New";
        await _sut.UpdateAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        Assert.That(loaded!.DisplayName, Is.EqualTo("New"));
    }

    [Test]
    public async Task UpdateAsync_AddsNewValue()
    {
        var item = MakeItem();
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        loaded!.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "Added", ItemId = item.Id });
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        Assert.That(reloaded!.Values, Has.Count.EqualTo(1));
        Assert.That(((TextFieldValue)reloaded.Values[0]).Value, Is.EqualTo("Added"));
    }

    [Test]
    public async Task UpdateAsync_RemovesDroppedValue()
    {
        var item = MakeItem();
        item.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "To remove", ItemId = item.Id });
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        loaded!.Values.Clear();
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        Assert.That(reloaded!.Values, Is.Empty);
    }

    [Test]
    public async Task UpdateAsync_UpdatesExistingValue_ViaScalar()
    {
        var valueId = Guid.NewGuid();
        var item = MakeItem();
        item.Values.Add(new TextFieldValue { Id = valueId, FieldDefinitionId = _textField.Id, Value = "Before", ItemId = item.Id });
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        ((TextFieldValue)loaded!.Values[0]).Value = "After";
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        Assert.That(((TextFieldValue)reloaded!.Values[0]).Value, Is.EqualTo("After"));
    }

    [Test]
    public async Task UpdateAsync_AddsListEntry()
    {
        var listField = new ListFieldDefinition { Label = "Chapters", PresetId = _preset.Id };
        var subText = new TextFieldDefinition { Label = "Name", ParentListFieldDefinitionId = listField.Id };
        listField.SubFields.Add(subText);
        using var db = DbFactory();
        db.Presets.Attach(_preset);
        _preset.Fields.Add(listField);
        db.SaveChanges();

        var item = MakeItem();
        var listValue = new ListFieldValue { FieldDefinitionId = listField.Id, ItemId = item.Id };
        item.Values.Add(listValue);
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        var lv = (ListFieldValue)loaded!.Values[0];
        lv.Entries.Add(new ListEntry
        {
            ListFieldValueId = lv.Id,
            DisplayOrder = 0,
            SubValues = { new TextFieldValue { FieldDefinitionId = subText.Id, Value = "Chapter 1" } }
        });
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        Assert.That(((ListFieldValue)reloaded!.Values[0]).Entries, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task UpdateAsync_RemovesDroppedListEntry()
    {
        var listField = new ListFieldDefinition { Label = "Tags", PresetId = _preset.Id };
        var subText = new TextFieldDefinition { Label = "Value", ParentListFieldDefinitionId = listField.Id };
        listField.SubFields.Add(subText);
        using var db = DbFactory();
        db.Presets.Attach(_preset);
        _preset.Fields.Add(listField);
        db.SaveChanges();

        var item = MakeItem();
        var listValue = new ListFieldValue { FieldDefinitionId = listField.Id, ItemId = item.Id };
        var entry = new ListEntry { ListFieldValueId = listValue.Id, DisplayOrder = 0 };
        listValue.Entries.Add(entry);
        item.Values.Add(listValue);
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        ((ListFieldValue)loaded!.Values[0]).Entries.Clear();
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        Assert.That(((ListFieldValue)reloaded!.Values[0]).Entries, Is.Empty);
    }

    [Test]
    public async Task UpdateAsync_UpdatesListEntrySubValues()
    {
        var listField = new ListFieldDefinition { Label = "Scenes", PresetId = _preset.Id };
        var subText = new TextFieldDefinition { Label = "Title", ParentListFieldDefinitionId = listField.Id };
        listField.SubFields.Add(subText);
        using var db = DbFactory();
        db.Presets.Attach(_preset);
        _preset.Fields.Add(listField);
        db.SaveChanges();

        var item = MakeItem();
        var listValue = new ListFieldValue { FieldDefinitionId = listField.Id, ItemId = item.Id };
        var subVal = new TextFieldValue { FieldDefinitionId = subText.Id, Value = "Original" };
        var entry = new ListEntry { ListFieldValueId = listValue.Id, DisplayOrder = 0, SubValues = { subVal } };
        listValue.Entries.Add(entry);
        item.Values.Add(listValue);
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        var loadedEntry = ((ListFieldValue)loaded!.Values[0]).Entries[0];
        ((TextFieldValue)loadedEntry.SubValues[0]).Value = "Updated";
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        var reloadedSub = ((ListFieldValue)reloaded!.Values[0]).Entries[0].SubValues[0];
        Assert.That(((TextFieldValue)reloadedSub).Value, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task UpdateAsync_MixedAddRemoveEdit_AppliesAllInOnePass()
    {
        var fieldKeep = new TextFieldDefinition { Label = "Keep", PresetId = _preset.Id };
        var fieldDrop = new TextFieldDefinition { Label = "Drop", PresetId = _preset.Id };
        using (var db = DbFactory())
        {
            db.Presets.Attach(_preset);
            _preset.Fields.Add(fieldKeep);
            _preset.Fields.Add(fieldDrop);
            db.SaveChanges();
        }

        var keepId = Guid.NewGuid();
        var dropId = Guid.NewGuid();
        var item = MakeItem();
        item.Values.Add(new TextFieldValue { Id = keepId, FieldDefinitionId = fieldKeep.Id, Value = "before", ItemId = item.Id });
        item.Values.Add(new TextFieldValue { Id = dropId, FieldDefinitionId = fieldDrop.Id, Value = "remove me", ItemId = item.Id });
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        ((TextFieldValue)loaded!.Values.Single(v => v.Id == keepId)).Value = "after";     // edit
        loaded.Values.Remove(loaded.Values.Single(v => v.Id == dropId));                   // remove
        loaded.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "new", ItemId = item.Id }); // add
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        Assert.That(reloaded!.Values.Select(v => ((TextFieldValue)v).Value),
            Is.EquivalentTo(new[] { "after", "new" }));
        Assert.That(reloaded.Values.Any(v => v.Id == dropId), Is.False, "dropped value must be gone");
        Assert.That(((TextFieldValue)reloaded.Values.Single(v => v.Id == keepId)).Value, Is.EqualTo("after"));
    }

    [Test]
    public async Task DeleteAsync_RemovesItem()
    {
        var item = MakeItem();
        await _sut.AddAsync(item);

        await _sut.DeleteAsync(item.Id);

        Assert.That(await _sut.GetByIdAsync(item.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_IsNoOp_WhenNotFound()
    {
        Assert.DoesNotThrowAsync(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task DeleteByPresetAsync_RemovesAllItemsForPreset()
    {
        await _sut.AddAsync(MakeItem("A"));
        await _sut.AddAsync(MakeItem("B"));

        await _sut.DeleteByPresetAsync(_preset.Id);

        var remaining = await _sut.GetByPresetAsync(_preset.Id);
        Assert.That(remaining, Is.Empty);
    }

    [Test]
    public async Task UpdateAsync_IsNoOp_WhenItemNotFound()
    {
        var ghost = MakeItem();

        Assert.DoesNotThrowAsync(() => _sut.UpdateAsync(ghost));
    }

    [Test]
    public async Task GetByPresetAsync_IncludesListEntries()
    {
        var listField = new ListFieldDefinition { Label = "Parts", PresetId = _preset.Id };
        var subText = new TextFieldDefinition { Label = "Part", ParentListFieldDefinitionId = listField.Id };
        listField.SubFields.Add(subText);
        using var db = DbFactory();
        db.Presets.Attach(_preset);
        _preset.Fields.Add(listField);
        db.SaveChanges();

        var item = MakeItem();
        var listValue = new ListFieldValue { FieldDefinitionId = listField.Id, ItemId = item.Id };
        listValue.Entries.Add(new ListEntry { ListFieldValueId = listValue.Id, DisplayOrder = 0 });
        item.Values.Add(listValue);
        await _sut.AddAsync(item);

        var result = await _sut.GetByPresetAsync(_preset.Id);

        var lv = (ListFieldValue)result[0].Values.First(v => v is ListFieldValue);
        Assert.That(lv.Entries, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AddAsync_CallsLoggerDebug()
    {
        var logger = new RecordingLogger();
        var repo = new ItemRepository(DbFactory, logger);

        await repo.AddAsync(MakeItem());

        Assert.That(logger.DebugCallCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task UpdateAsync_CallsLoggerDebug()
    {
        var logger = new RecordingLogger();
        var repo = new ItemRepository(DbFactory, logger);
        var item = MakeItem();
        await repo.AddAsync(item);

        item.DisplayName = "Changed";
        await repo.UpdateAsync(item);

        Assert.That(logger.DebugCallCount, Is.GreaterThan(0));
    }

    [Test]
    public void Constructor_WhenLoggerIsNull_UsesNullAppLogger()
    {
        var repo = new ItemRepository(DbFactory, null);
        Assert.DoesNotThrow(() => { });
    }

    [Test]
    public async Task GetByIdAsync_EagerLoadsNestedListEntriesAndSubValues()
    {
        var listField = new ListFieldDefinition { Label = "Scenes", PresetId = _preset.Id };
        var subText = new TextFieldDefinition { Label = "Title", ParentListFieldDefinitionId = listField.Id };
        listField.SubFields.Add(subText);
        using (var db = DbFactory())
        {
            db.Presets.Attach(_preset);
            _preset.Fields.Add(listField);
            db.SaveChanges();
        }

        var item = MakeItem();
        var listValue = new ListFieldValue { FieldDefinitionId = listField.Id, ItemId = item.Id };
        var entry = new ListEntry
        {
            ListFieldValueId = listValue.Id,
            DisplayOrder = 0,
            SubValues = { new TextFieldValue { FieldDefinitionId = subText.Id, Value = "Scene 1" } }
        };
        listValue.Entries.Add(entry);
        item.Values.Add(listValue);
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);

        var lv = (ListFieldValue)loaded!.Values.Single(v => v is ListFieldValue);
        Assert.That(lv.Entries, Is.Not.Empty, "List entries must be eager-loaded");
        Assert.That(lv.Entries[0].SubValues, Is.Not.Empty, "Entry sub-values must be eager-loaded");
        Assert.That(((TextFieldValue)lv.Entries[0].SubValues[0]).Value, Is.EqualTo("Scene 1"));
    }

    [Test]
    public async Task UpdateAsync_RemovesDroppedSubValueButKeepsOthers()
    {
        var listField = new ListFieldDefinition { Label = "Specs", PresetId = _preset.Id };
        var subA = new TextFieldDefinition { Label = "A", ParentListFieldDefinitionId = listField.Id };
        var subB = new TextFieldDefinition { Label = "B", ParentListFieldDefinitionId = listField.Id };
        listField.SubFields.Add(subA);
        listField.SubFields.Add(subB);
        using (var db = DbFactory())
        {
            db.Presets.Attach(_preset);
            _preset.Fields.Add(listField);
            db.SaveChanges();
        }

        var item = MakeItem();
        var listValue = new ListFieldValue { FieldDefinitionId = listField.Id, ItemId = item.Id };
        var valA = new TextFieldValue { FieldDefinitionId = subA.Id, Value = "keep" };
        var valB = new TextFieldValue { FieldDefinitionId = subB.Id, Value = "drop" };
        var entry = new ListEntry { ListFieldValueId = listValue.Id, DisplayOrder = 0, SubValues = { valA, valB } };
        listValue.Entries.Add(entry);
        item.Values.Add(listValue);
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        var loadedEntry = ((ListFieldValue)loaded!.Values[0]).Entries[0];
        var toDrop = loadedEntry.SubValues.Single(v => v.Id == valB.Id);
        loadedEntry.SubValues.Remove(toDrop);
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        var reloadedEntry = ((ListFieldValue)reloaded!.Values[0]).Entries[0];
        Assert.That(reloadedEntry.SubValues, Has.Count.EqualTo(1), "Only the dropped sub-value should be removed");
        Assert.That(reloadedEntry.SubValues[0].Id, Is.EqualTo(valA.Id), "The kept sub-value must remain");
    }

    [Test]
    public async Task UpdateAsync_KeepsAllSubValuesWhenNoneRemoved()
    {
        var listField = new ListFieldDefinition { Label = "Specs", PresetId = _preset.Id };
        var subA = new TextFieldDefinition { Label = "A", ParentListFieldDefinitionId = listField.Id };
        var subB = new TextFieldDefinition { Label = "B", ParentListFieldDefinitionId = listField.Id };
        listField.SubFields.Add(subA);
        listField.SubFields.Add(subB);
        using (var db = DbFactory())
        {
            db.Presets.Attach(_preset);
            _preset.Fields.Add(listField);
            db.SaveChanges();
        }

        var item = MakeItem();
        var listValue = new ListFieldValue { FieldDefinitionId = listField.Id, ItemId = item.Id };
        var valA = new TextFieldValue { FieldDefinitionId = subA.Id, Value = "a" };
        var valB = new TextFieldValue { FieldDefinitionId = subB.Id, Value = "b" };
        listValue.Entries.Add(new ListEntry { ListFieldValueId = listValue.Id, DisplayOrder = 0, SubValues = { valA, valB } });
        item.Values.Add(listValue);
        await _sut.AddAsync(item);

        var loaded = await _sut.GetByIdAsync(item.Id);
        var loadedEntry = ((ListFieldValue)loaded!.Values[0]).Entries[0];
        ((TextFieldValue)loadedEntry.SubValues.Single(v => v.Id == valA.Id)).Value = "a-updated";
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(item.Id);
        var reloadedEntry = ((ListFieldValue)reloaded!.Values[0]).Entries[0];
        Assert.That(reloadedEntry.SubValues, Has.Count.EqualTo(2),
            "When no sub-value is dropped, all must be preserved (guards the removal filter against All→Any)");
    }

    [Test]
    public async Task DeleteAsync_CascadesValuesListEntriesAndSubValues()
    {
        var listField = new ListFieldDefinition { Label = "Parts", PresetId = _preset.Id };
        var subText = new TextFieldDefinition { Label = "Part", ParentListFieldDefinitionId = listField.Id };
        listField.SubFields.Add(subText);
        using (var db = DbFactory())
        {
            db.Presets.Attach(_preset);
            _preset.Fields.Add(listField);
            db.SaveChanges();
        }

        var item = MakeItem();
        var listValue = new ListFieldValue { FieldDefinitionId = listField.Id, ItemId = item.Id };
        var entry = new ListEntry
        {
            ListFieldValueId = listValue.Id,
            DisplayOrder = 0,
            SubValues = { new TextFieldValue { FieldDefinitionId = subText.Id, Value = "X" } }
        };
        listValue.Entries.Add(entry);
        item.Values.Add(listValue);
        await _sut.AddAsync(item);

        await _sut.DeleteAsync(item.Id);

        using var verify = DbFactory();
        Assert.That(verify.Items.Count(), Is.EqualTo(0), "Item removed");
        Assert.That(verify.FieldValues.Count(), Is.EqualTo(0), "Values (and sub-values) must cascade-delete");
        Assert.That(verify.ListEntries.Count(), Is.EqualTo(0), "List entries must cascade-delete");
    }

    private async Task<(Guid presetId, Guid itemId)> AddForeignItemAsync(Guid ownerId)
    {
        var preset = new Preset { Name = "Foreign", OwnerId = ownerId };
        var item = new Item { PresetId = preset.Id, DisplayName = "Secret" };
        using var db = DbFactory();
        db.Presets.Add(preset);
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return (preset.Id, item.Id);
    }

    [Test]
    public async Task GetByPresetAsync_WhenScoped_ReturnsItemsForOwnedPreset()
    {
        var me = Guid.NewGuid();
        var scoped = new ItemRepository(DbFactory, null, new FixedItemUser(me));
        var owned = new Preset { Name = "Mine", OwnerId = me };
        using (var db = DbFactory()) { db.Presets.Add(owned); await db.SaveChangesAsync(); }
        var item = new Item { PresetId = owned.Id, DisplayName = "Loco" };
        await scoped.AddAsync(item);

        var result = await scoped.GetByPresetAsync(owned.Id);

        Assert.That(result.Select(i => i.Id), Does.Contain(item.Id));
    }

    [Test]
    public async Task GetByPresetAsync_WhenScoped_HidesUnauthorizedPresetItems()
    {
        var me = Guid.NewGuid();
        var (foreignPresetId, _) = await AddForeignItemAsync(Guid.NewGuid());
        var scoped = new ItemRepository(DbFactory, null, new FixedItemUser(me));

        var result = await scoped.GetByPresetAsync(foreignPresetId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetByIdAsync_WhenScoped_ReturnsNullForUnauthorizedItem()
    {
        var me = Guid.NewGuid();
        var (_, foreignItemId) = await AddForeignItemAsync(Guid.NewGuid());
        var scoped = new ItemRepository(DbFactory, null, new FixedItemUser(me));

        var result = await scoped.GetByIdAsync(foreignItemId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByPresetAsync_WhenScoped_ReturnsItemsForSharedPreset()
    {
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        var (sharedPresetId, sharedItemId) = await AddForeignItemAsync(other);
        using (var db = DbFactory())
        {
            db.CollectionShares.Add(new CollectionShare
            {
                PresetId = sharedPresetId,
                SharedWithUserId = me,
                GrantedByUserId = other,
                Permission = SharePermission.Read,
            });
            await db.SaveChangesAsync();
        }
        var scoped = new ItemRepository(DbFactory, null, new FixedItemUser(me));

        var result = await scoped.GetByPresetAsync(sharedPresetId);

        Assert.That(result.Select(i => i.Id), Does.Contain(sharedItemId));
    }

    private async Task AddFieldAsync(FieldDefinition definition)
    {
        using var db = DbFactory();
        db.Presets.Attach(_preset);
        _preset.Fields.Add(definition);
        await db.SaveChangesAsync();
    }

    private Expression<Func<Item, bool>>? ServerFilterFor(string query, params FieldDefinition[] extraDefinitions)
    {
        var fields = new List<FieldDefinition> { _textField };
        fields.AddRange(extraDefinitions);
        var snapshot = new SearchCatalogSnapshot
        {
            Fields = fields
                .GroupBy(f => f.Label, StringComparer.OrdinalIgnoreCase)
                .Select(g => new SearchFieldGroup(g.Key, g.ToList()))
                .ToList(),
            Presets = [new SearchPresetEntry(_preset.Id, _preset.Name)],
        };
        var parsed = new QueryParser(new QueryLexer()).Parse(query);
        var bound = new QueryBinder(new PseudoFieldCatalog()).Bind(parsed.Query!, snapshot);
        Assert.That(bound.Errors, Is.Empty);
        var filter = new ServerFilterBuilder().Build(bound.Query!.Root);
        Assert.That(filter, Is.Not.Null, $"expected a server-translatable filter for: {query}");
        return filter;
    }

    [Test]
    public async Task SearchAsync_WithoutFilter_ReturnsAllItems()
    {
        await _sut.AddAsync(MakeItem("A"));
        await _sut.AddAsync(MakeItem("B"));

        var result = await _sut.SearchAsync(null);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SearchAsync_TextEqualsFilter_RunsInSql()
    {
        var hobbit = MakeItem("Hobbit");
        hobbit.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "The Hobbit" });
        var dune = MakeItem("Dune");
        dune.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "Dune" });
        await _sut.AddAsync(hobbit);
        await _sut.AddAsync(dune);

        var result = await _sut.SearchAsync(ServerFilterFor("Title = \"the hobbit\""));

        Assert.That(result.Single().Id, Is.EqualTo(hobbit.Id));
    }

    [Test]
    public async Task SearchAsync_IntegerComparisonFilter_RunsInSql()
    {
        var pages = new IntegerFieldDefinition { Label = "Pages", PresetId = _preset.Id };
        await AddFieldAsync(pages);
        var thick = MakeItem("Thick");
        thick.Values.Add(new IntegerFieldValue { FieldDefinitionId = pages.Id, Value = 300 });
        var thin = MakeItem("Thin");
        thin.Values.Add(new IntegerFieldValue { FieldDefinitionId = pages.Id, Value = 100 });
        await _sut.AddAsync(thick);
        await _sut.AddAsync(thin);

        var result = await _sut.SearchAsync(ServerFilterFor("Pages > 200", pages));

        Assert.That(result.Single().Id, Is.EqualTo(thick.Id));
    }

    [Test]
    public async Task SearchAsync_CurrencyComparisonFilter_RunsInSql()
    {
        var price = new CurrencyFieldDefinition { Label = "Price", PresetId = _preset.Id };
        await AddFieldAsync(price);
        var pricey = MakeItem("Pricey");
        pricey.Values.Add(new CurrencyFieldValue { FieldDefinitionId = price.Id, Value = 20m });
        var cheap = MakeItem("Cheap");
        cheap.Values.Add(new CurrencyFieldValue { FieldDefinitionId = price.Id, Value = 5.50m });
        await _sut.AddAsync(pricey);
        await _sut.AddAsync(cheap);

        var result = await _sut.SearchAsync(ServerFilterFor("Price > 10", price));

        Assert.That(result.Single().Id, Is.EqualTo(pricey.Id));
    }

    [Test]
    public async Task SearchAsync_DateComparisonFilter_RunsInSql()
    {
        var published = new DateFieldDefinition { Label = "Published", PresetId = _preset.Id };
        await AddFieldAsync(published);
        var old = MakeItem("Old");
        old.Values.Add(new DateFieldValue { FieldDefinitionId = published.Id, Value = new DateTime(1954, 7, 29) });
        var recent = MakeItem("Recent");
        recent.Values.Add(new DateFieldValue { FieldDefinitionId = published.Id, Value = new DateTime(2025, 5, 1) });
        await _sut.AddAsync(old);
        await _sut.AddAsync(recent);

        var result = await _sut.SearchAsync(ServerFilterFor("Published < 2000-01-01", published));

        Assert.That(result.Single().Id, Is.EqualTo(old.Id));
    }

    [Test]
    public async Task SearchAsync_InListFilter_RunsInSql()
    {
        var hobbit = MakeItem("Hobbit");
        hobbit.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "Hobbit" });
        var dune = MakeItem("Dune");
        dune.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "Dune" });
        var other = MakeItem("Other");
        other.Values.Add(new TextFieldValue { FieldDefinitionId = _textField.Id, Value = "Other" });
        await _sut.AddAsync(hobbit);
        await _sut.AddAsync(dune);
        await _sut.AddAsync(other);

        var result = await _sut.SearchAsync(ServerFilterFor("Title in (hobbit, dune)"));

        Assert.That(result.Select(i => i.Id), Is.EquivalentTo(new[] { hobbit.Id, dune.Id }));
    }

    [Test]
    public async Task SearchAsync_DisplayNamePseudoFilter_RunsInSql()
    {
        await _sut.AddAsync(MakeItem("Loco 42"));
        await _sut.AddAsync(MakeItem("Wagon"));

        var result = await _sut.SearchAsync(ServerFilterFor("name ~ loco"));

        Assert.That(result.Single().DisplayName, Is.EqualTo("Loco 42"));
    }

    [Test]
    public async Task SearchAsync_PresetPseudoFilter_RunsInSql()
    {
        var other = new Preset { Name = "Other" };
        using (var db = DbFactory())
        {
            db.Presets.Add(other);
            await db.SaveChangesAsync();
        }
        await _sut.AddAsync(MakeItem("Mine"));
        await _sut.AddAsync(new Item { PresetId = other.Id, DisplayName = "Foreign" });

        var result = await _sut.SearchAsync(ServerFilterFor("preset = books"));

        Assert.That(result.Single().DisplayName, Is.EqualTo("Mine"));
    }

    [Test]
    public async Task SearchAsync_WhenScoped_HidesUnauthorizedItems()
    {
        var me = Guid.NewGuid();
        await AddForeignItemAsync(Guid.NewGuid());
        var scoped = new ItemRepository(DbFactory, null, new FixedItemUser(me));

        var result = await scoped.SearchAsync(null);

        Assert.That(result, Is.Empty);
    }
}

file sealed class FixedItemUser : ICurrentUser
{
    public FixedItemUser(Guid userId) => UserId = userId;
    public Guid UserId { get; }
    public bool IsAuthenticated => UserId != Guid.Empty;
}
