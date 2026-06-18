using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class AutoNumberFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(Guid? editingItemId = null) =>
        new(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask)
        { EditingItemId = editingItemId };

    private static IAutoNumberService Service(IReadOnlyCollection<int>? used = null)
    {
        var service = A.Fake<IAutoNumberService>();
        A.CallTo(() => service.UsedNumbersAsync(A<Guid>._, A<Guid?>._)).Returns(used ?? Array.Empty<int>());
        return service;
    }

    private static AutoNumberFieldEditorViewModel Make(
        AutoNumberFieldDefinition def, AutoNumberFieldValue value,
        IReadOnlyCollection<int>? used = null, Guid? editingItemId = null) =>
        new(def, value, MakeContext(editingItemId), Service(used));

    [Test]
    public async Task NewItem_LeftBlank_GeneratesNextNumberOnSave()
    {
        var sut = Make(new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.HighestPlusOne },
            new AutoNumberFieldValue(), used: new[] { 1, 2, 5 }, editingItemId: null);
        await sut.Ready;

        Assert.That(sut.Number, Is.Null, "the box stays empty at open — no pre-fill, the watermark explains it generates on save");
        Assert.That(((AutoNumberFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(6), "the next number is assigned on save");
    }

    [Test]
    public async Task NewItem_WithTypedValue_KeepsWhatTheUserTyped()
    {
        var sut = Make(new AutoNumberFieldDefinition { Editable = true }, new AutoNumberFieldValue(), used: new[] { 1, 2, 5 });
        await sut.Ready;

        sut.Number = 42;

        Assert.That(((AutoNumberFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(42));
    }

    [Test]
    public async Task ExistingItem_WithNoNumber_StaysEmpty_NeverGenerates([Values(true, false)] bool editable)
    {
        var sut = Make(new AutoNumberFieldDefinition { Editable = editable },
            new AutoNumberFieldValue(), used: new[] { 1, 2, 5 }, editingItemId: Guid.NewGuid());
        await sut.Ready;

        Assert.Multiple(() =>
        {
            Assert.That(sut.Number, Is.Null);
            Assert.That(((AutoNumberFieldValue)sut.GetCurrentValue()).Value, Is.Null,
                "an existing/imported item with no number is never given one");
            Assert.That(sut.HasNotice, Is.False);
            Assert.That(sut.Validate(), Is.Null);
        });
    }

    [Test]
    public async Task ExistingItem_KeepsStoredValue()
    {
        var sut = Make(new AutoNumberFieldDefinition(),
            new AutoNumberFieldValue { Value = 10 }, used: new[] { 1, 2 }, editingItemId: Guid.NewGuid());
        await sut.Ready;

        Assert.That(sut.Number, Is.EqualTo(10));
    }

    [Test]
    public void Watermark_IsShownForANewItem_AndAbsentForAnExistingOne()
    {
        Assert.That(Make(new AutoNumberFieldDefinition(), new AutoNumberFieldValue(), editingItemId: null).Watermark,
            Is.Not.Null.And.Not.Empty);
        Assert.That(Make(new AutoNumberFieldDefinition(), new AutoNumberFieldValue(), editingItemId: Guid.NewGuid()).Watermark,
            Is.Null);
    }

    [Test]
    public void IsEditable_ReflectsDefinition()
    {
        Assert.That(Make(new AutoNumberFieldDefinition { Editable = true }, new AutoNumberFieldValue()).IsEditable, Is.True);
        Assert.That(Make(new AutoNumberFieldDefinition { Editable = false }, new AutoNumberFieldValue()).IsEditable, Is.False);
    }

    [Test]
    public async Task Editable_Error_Duplicate_ShowsErrorAndBlocksSave()
    {
        var sut = Make(new AutoNumberFieldDefinition { Editable = true, OnDuplicate = DuplicateHandling.Error },
            new AutoNumberFieldValue { Value = 1 }, used: new[] { 5 }, editingItemId: Guid.NewGuid());
        await sut.Ready;

        sut.Number = 5;

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasDuplicate, Is.True);
            Assert.That(sut.HasNotice, Is.True);
            Assert.That(sut.NoticeIsError, Is.True);
            Assert.That(sut.NoticeText, Is.EqualTo(LocalizationService.Instance["AutoNumber_DuplicateNotice"]));
            Assert.That(sut.Validate(), Is.EqualTo(LocalizationService.Instance["AutoNumber_DuplicateNotice"]));
        });
    }

    [Test]
    public async Task Editable_Warn_Duplicate_WarnsButAllowsSave()
    {
        var sut = Make(new AutoNumberFieldDefinition { Editable = true, OnDuplicate = DuplicateHandling.Warn },
            new AutoNumberFieldValue { Value = 1 }, used: new[] { 5 }, editingItemId: Guid.NewGuid());
        await sut.Ready;

        sut.Number = 5;

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasNotice, Is.True);
            Assert.That(sut.NoticeIsError, Is.False);
            Assert.That(sut.Validate(), Is.Null);
        });
    }

    [Test]
    public async Task Editable_Allow_Duplicate_NoNoticeNoBlock()
    {
        var sut = Make(new AutoNumberFieldDefinition { Editable = true, OnDuplicate = DuplicateHandling.Allow },
            new AutoNumberFieldValue { Value = 1 }, used: new[] { 5 }, editingItemId: Guid.NewGuid());
        await sut.Ready;

        sut.Number = 5;

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasNotice, Is.False);
            Assert.That(sut.Validate(), Is.Null);
        });
    }

    [Test]
    public async Task ReadOnly_WithCollision_ShowsNoNoticeAndNeverBlocks(
        [Values(DuplicateHandling.Error, DuplicateHandling.Warn, DuplicateHandling.Allow)] DuplicateHandling mode)
    {
        var sut = Make(new AutoNumberFieldDefinition { Editable = false, OnDuplicate = mode },
            new AutoNumberFieldValue { Value = 5 }, used: new[] { 5 }, editingItemId: Guid.NewGuid());
        await sut.Ready;

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasDuplicate, Is.True);
            Assert.That(sut.HasNotice, Is.False);
            Assert.That(sut.Validate(), Is.Null);
        });
    }

    [Test]
    public async Task WhenUsedNumberLookupFails_KeepsExistingValueAndDoesNotBlock()
    {
        var service = A.Fake<IAutoNumberService>();
        A.CallTo(() => service.UsedNumbersAsync(A<Guid>._, A<Guid?>._)).Throws(new InvalidOperationException("boom"));
        var sut = new AutoNumberFieldEditorViewModel(
            new AutoNumberFieldDefinition(), new AutoNumberFieldValue { Value = 3 }, MakeContext(Guid.NewGuid()), service);

        await sut.Ready;

        Assert.Multiple(() =>
        {
            Assert.That(sut.Number, Is.EqualTo(3));
            Assert.That(sut.Validate(), Is.Null);
        });
    }
}
