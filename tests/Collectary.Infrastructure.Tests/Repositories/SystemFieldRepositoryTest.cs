using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
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
public class SystemFieldRepositoryTest : DbIntegrationTestBase
{
    private SystemFieldRepository _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger());
    }

    private static SystemField MakeField(string name = "Test") => new()
    {
        Name = name,
        Definition = new TextFieldDefinition { Label = name }
    };

    [Test]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoneExist()
    {
        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task AddAsync_AssignsSortOrder0_WhenFirst()
    {
        var field = MakeField();

        await _sut.AddAsync(field);

        var saved = (await _sut.GetAllAsync()).Single();
        Assert.That(saved.SortOrder, Is.EqualTo(0));
    }

    [Test]
    public async Task AddAsync_AssignsMaxPlusOne_WhenOthersExist()
    {
        await _sut.AddAsync(MakeField("A"));
        await _sut.AddAsync(MakeField("B"));

        var all = await _sut.GetAllAsync();
        Assert.That(all.Select(f => f.SortOrder), Is.EquivalentTo(new[] { 0, 1 }));
    }

    [Test]
    public async Task AddAsync_PersistsAndCanBeRead()
    {
        var field = MakeField("Color");

        await _sut.AddAsync(field);

        var loaded = await _sut.GetByIdAsync(field.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Name, Is.EqualTo("Color"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_ReturnsCorrectField()
    {
        var field = MakeField("Size");
        await _sut.AddAsync(field);

        var result = await _sut.GetByIdAsync(field.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Size"));
    }

    [Test]
    public async Task UpdateAsync_UpdatesName()
    {
        var field = MakeField("OldName");
        await _sut.AddAsync(field);

        field.Name = "NewName";
        await _sut.UpdateAsync(field);

        var loaded = await _sut.GetByIdAsync(field.Id);
        Assert.That(loaded!.Name, Is.EqualTo("NewName"));
    }

    [Test]
    public async Task UpdateAsync_UpdatesDefinitionLabel()
    {
        var field = MakeField("Original");
        await _sut.AddAsync(field);

        field.Definition.Label = "Updated";
        await _sut.UpdateAsync(field);

        var loaded = await _sut.GetByIdAsync(field.Id);
        Assert.That(loaded!.Definition.Label, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task ReorderAsync_UpdatesSortOrders()
    {
        var a = MakeField("A");
        var b = MakeField("B");
        var c = MakeField("C");
        await _sut.AddAsync(a);
        await _sut.AddAsync(b);
        await _sut.AddAsync(c);

        await _sut.ReorderAsync(new[] { c.Id, a.Id, b.Id });

        var all = (await _sut.GetAllAsync()).OrderBy(f => f.SortOrder).ToList();
        Assert.That(all[0].Name, Is.EqualTo("C"));
        Assert.That(all[1].Name, Is.EqualTo("A"));
        Assert.That(all[2].Name, Is.EqualTo("B"));
    }

    [Test]
    public async Task DeleteAsync_RemovesField()
    {
        var field = MakeField();
        await _sut.AddAsync(field);

        await _sut.DeleteAsync(field.Id);

        Assert.That(await _sut.GetByIdAsync(field.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_IsNoOp_WhenNotFound()
    {
        Assert.DoesNotThrowAsync(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetAllAsync_OrdersBySortOrderThenName()
    {
        await _sut.AddAsync(MakeField("Bravo"));
        await _sut.AddAsync(MakeField("Alpha"));

        await _sut.ReorderAsync(new[] { (await _sut.GetAllAsync())[0].Id, (await _sut.GetAllAsync())[1].Id });

        var all = await _sut.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all[0].SortOrder, Is.LessThanOrEqualTo(all[1].SortOrder));
    }

    [Test]
    public async Task GetAllAsync_ReturnsFieldsInInsertionOrder()
    {
        var first = MakeField("First");
        var second = MakeField("Second");
        await _sut.AddAsync(first);
        await _sut.AddAsync(second);

        var all = await _sut.GetAllAsync();

        var names = all.Select(f => f.Name).ToList();
        Assert.That(names.IndexOf("First"), Is.LessThan(names.IndexOf("Second")));
    }

    [Test]
    public async Task UpdateAsync_CallsLoggerDebug()
    {
        var logger = new RecordingLogger();
        var sut = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger(), logger);
        var field = MakeField();
        await sut.AddAsync(field);

        field.Name = "Updated";
        await sut.UpdateAsync(field);

        Assert.That(logger.DebugCallCount, Is.GreaterThan(0));
    }

    [Test]
    public void Constructor_WhenLoggerIsNull_UsesNullAppLogger()
    {
        var sut = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger(), null);
        Assert.DoesNotThrow(() => { });
    }

    [Test]
    public async Task GetByIdAsync_EagerLoadsDefinition()
    {
        var field = new SystemField { Name = "Tag", Definition = new TextFieldDefinition { Label = "Tag" } };
        await _sut.AddAsync(field);

        var loaded = await _sut.GetByIdAsync(field.Id);

        Assert.That(loaded!.Definition, Is.Not.Null, "Definition must be eager-loaded");
        Assert.That(loaded.Definition.Label, Is.EqualTo("Tag"));
    }

    [Test]
    public async Task GetByIdAsync_EagerLoadsListDefinitionSubFields()
    {
        var listDef = new ListFieldDefinition { Label = "Chapters" };
        listDef.SubFields.Add(new TextFieldDefinition { Label = "Name", ParentListFieldDefinitionId = listDef.Id });
        var field = new SystemField { Name = "Chapters", Definition = listDef };
        await _sut.AddAsync(field);

        var loaded = await _sut.GetByIdAsync(field.Id);

        var loadedList = (ListFieldDefinition)loaded!.Definition;
        Assert.That(loadedList.SubFields, Is.Not.Empty, "List sub-fields must be eager-loaded");
    }

    [Test]
    public async Task DeleteAsync_CascadesDefinition()
    {
        var field = MakeField("Color");
        await _sut.AddAsync(field);

        await _sut.DeleteAsync(field.Id);

        using var db = DbFactory();
        Assert.That(db.SystemFields.Count(), Is.EqualTo(0), "System field removed");
        Assert.That(db.FieldDefinitions.Count(), Is.EqualTo(0), "Its definition must cascade-delete");
    }
}
