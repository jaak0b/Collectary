using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class ItemUseCaseTest
{
    private IItemRepository _items = null!;
    private IPresetUseCase _presets = null!;
    private ItemUseCase _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _items = A.Fake<IItemRepository>();
        _presets = A.Fake<IPresetUseCase>();
        _sut = new ItemUseCase(_items, _presets);
    }

    [Test]
    public async Task GetItemsForPresetAsync_ReturnsRepositoryResult()
    {
        var presetId = Guid.NewGuid();
        var itemList = new List<Item> { new() { DisplayName = "X" } };
        A.CallTo(() => _items.GetByPresetAsync(presetId)).Returns(itemList);

        var result = await _sut.GetItemsForPresetAsync(presetId);

        Assert.That(result, Is.EqualTo(itemList));
    }

    [Test]
    public async Task GetItemAsync_ReturnsRepositoryResult()
    {
        var id = Guid.NewGuid();
        var item = new Item { DisplayName = "X" };
        A.CallTo(() => _items.GetByIdAsync(id)).Returns(item);

        var result = await _sut.GetItemAsync(id);

        Assert.That(result, Is.EqualTo(item));
    }

    [Test]
    public async Task DeleteItemAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();

        await _sut.DeleteItemAsync(id);

        A.CallTo(() => _items.DeleteAsync(id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task CreateItemAsync_CallsAddWhenRequiredFieldsPresent()
    {
        var presetId = Guid.NewGuid();
        var displayNameDef = new DisplayNameFieldDefinition { IsRequired = true };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { displayNameDef } });

        var item = new Item { PresetId = presetId, DisplayName = "My Item" };

        await _sut.CreateItemAsync(item);

        A.CallTo(() => _items.AddAsync(item)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void CreateItemAsync_ThrowsWhenRequiredDisplayNameMissing()
    {
        var presetId = Guid.NewGuid();
        var displayNameDef = new DisplayNameFieldDefinition { IsRequired = true };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { displayNameDef } });

        var item = new Item { PresetId = presetId, DisplayName = "" };

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateItemAsync(item));
    }

    [Test]
    public void CreateItemAsync_ThrowsWhenRequiredTextFieldMissing()
    {
        var presetId = Guid.NewGuid();
        var fieldDef = new TextFieldDefinition { Id = Guid.NewGuid(), IsRequired = true, Label = "Notes" };
        var displayNameDef = new DisplayNameFieldDefinition { IsRequired = false };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { displayNameDef, fieldDef } });

        var item = new Item
        {
            PresetId = presetId,
            DisplayName = "My Item",
            Values = []
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateItemAsync(item));
        Assert.That(ex!.Message, Does.Contain("Notes"));
    }

    [Test]
    public async Task CreateItemAsync_PassesWhenRequiredTextFieldHasValue()
    {
        var presetId = Guid.NewGuid();
        var fieldDefId = Guid.NewGuid();
        var fieldDef = new TextFieldDefinition { Id = fieldDefId, IsRequired = true, Label = "Notes" };
        var displayNameDef = new DisplayNameFieldDefinition { IsRequired = false };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { displayNameDef, fieldDef } });

        var item = new Item
        {
            PresetId = presetId,
            DisplayName = "My Item",
            Values = [new TextFieldValue { FieldDefinitionId = fieldDefId, Value = "some text" }]
        };

        await _sut.CreateItemAsync(item);

        A.CallTo(() => _items.AddAsync(item)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task UpdateItemAsync_CallsUpdateWhenRequiredFieldsPresent()
    {
        var presetId = Guid.NewGuid();
        var displayNameDef = new DisplayNameFieldDefinition { IsRequired = true };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { displayNameDef } });

        var item = new Item { PresetId = presetId, DisplayName = "Updated Item" };

        await _sut.UpdateItemAsync(item);

        A.CallTo(() => _items.UpdateAsync(item)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void UpdateItemAsync_ThrowsWhenRequiredDisplayNameMissing()
    {
        var presetId = Guid.NewGuid();
        var displayNameDef = new DisplayNameFieldDefinition { IsRequired = true };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { displayNameDef } });

        var item = new Item { PresetId = presetId, DisplayName = "   " };

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateItemAsync(item));
    }

    [Test]
    public async Task CreateItemAsync_SetsUpdatedAtTimestamp()
    {
        var presetId = Guid.NewGuid();
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields());

        var before = DateTime.UtcNow.AddSeconds(-1);
        var item = new Item { PresetId = presetId };

        await _sut.CreateItemAsync(item);

        Assert.That(item.UpdatedAt, Is.GreaterThan(before));
    }

    [Test]
    public async Task CreateItemAsync_DoesNotThrowWhenNonRequiredFieldHasNoValue()
    {
        var presetId = Guid.NewGuid();
        var requiredField = new TextFieldDefinition { Id = Guid.NewGuid(), IsRequired = true, Label = "R" };
        var optionalField = new TextFieldDefinition { Id = Guid.NewGuid(), IsRequired = false, Label = "O" };
        var dnDef = new DisplayNameFieldDefinition { IsRequired = false };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields
            {
                Fields = new List<FieldDefinition> { dnDef, requiredField, optionalField }
            });

        var item = new Item
        {
            PresetId = presetId,
            DisplayName = "Item",
            Values = [new TextFieldValue { FieldDefinitionId = requiredField.Id, Value = "filled" }]
        };

        Assert.DoesNotThrowAsync(() => _sut.CreateItemAsync(item));
    }

    [Test]
    public void CreateItemAsync_ThrowsWithDisplayNameInMessage_WhenDisplayNameMissing()
    {
        var presetId = Guid.NewGuid();
        var displayNameDef = new DisplayNameFieldDefinition { IsRequired = true };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { displayNameDef } });

        var item = new Item { PresetId = presetId, DisplayName = "" };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateItemAsync(item));
        Assert.That(ex!.Message, Does.Contain("Display Name"));
    }

    [Test]
    public async Task CreateItemAsync_CallsLoggerDebug()
    {
        var logger = A.Fake<IAppLogger>();
        var sut = new ItemUseCase(_items, _presets, logger);
        var presetId = Guid.NewGuid();
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId)).Returns(new EffectiveFields());

        await sut.CreateItemAsync(new Item { PresetId = presetId });

        A.CallTo(() => logger.Debug(A<string>._, A<object?[]>._)).MustHaveHappened();
    }

    [Test]
    public async Task UpdateItemAsync_CallsLoggerDebug()
    {
        var logger = A.Fake<IAppLogger>();
        var sut = new ItemUseCase(_items, _presets, logger);
        var presetId = Guid.NewGuid();
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId)).Returns(new EffectiveFields());

        await sut.UpdateItemAsync(new Item { PresetId = presetId });

        A.CallTo(() => logger.Debug(A<string>._, A<object?[]>._)).MustHaveHappened();
    }

    [Test]
    public async Task DeleteItemAsync_CallsLoggerDebug()
    {
        var logger = A.Fake<IAppLogger>();
        var sut = new ItemUseCase(_items, _presets, logger);

        await sut.DeleteItemAsync(Guid.NewGuid());

        A.CallTo(() => logger.Debug(A<string>._, A<object?[]>._)).MustHaveHappened();
    }

    [Test]
    public void Constructor_WhenLoggerIsNull_UsesNullAppLogger()
    {
        var sut = new ItemUseCase(_items, _presets, null);

        Assert.DoesNotThrowAsync(async () =>
        {
            var presetId = Guid.NewGuid();
            A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId)).Returns(new EffectiveFields());
            await sut.CreateItemAsync(new Item { PresetId = presetId });
        });
    }

    [Test]
    public void CreateItemAsync_WhenNotAuthorized_Throws()
    {
        var presetId = Guid.NewGuid();
        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.CanWriteAsync(presetId)).Returns(false);
        var sut = new ItemUseCase(_items, _presets, null, auth);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.CreateItemAsync(new Item { PresetId = presetId }));
    }

    [Test]
    public void UpdateItemAsync_WhenNotAuthorized_Throws()
    {
        var presetId = Guid.NewGuid();
        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.CanWriteAsync(presetId)).Returns(false);
        var sut = new ItemUseCase(_items, _presets, null, auth);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.UpdateItemAsync(new Item { PresetId = presetId }));
    }

    [Test]
    public void DeleteItemAsync_WhenNotAuthorized_Throws()
    {
        var presetId = Guid.NewGuid();
        var id = Guid.NewGuid();
        A.CallTo(() => _items.GetByIdAsync(id)).Returns(new Item { Id = id, PresetId = presetId });
        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.CanWriteAsync(presetId)).Returns(false);
        var sut = new ItemUseCase(_items, _presets, null, auth);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.DeleteItemAsync(id));
    }

    [Test]
    public async Task CreateItemAsync_WhenAuthorized_Adds()
    {
        var presetId = Guid.NewGuid();
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId)).Returns(new EffectiveFields());
        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.CanWriteAsync(presetId)).Returns(true);
        var sut = new ItemUseCase(_items, _presets, null, auth);
        var item = new Item { PresetId = presetId };

        await sut.CreateItemAsync(item);

        A.CallTo(() => _items.AddAsync(item)).MustHaveHappenedOnceExactly();
    }
}
