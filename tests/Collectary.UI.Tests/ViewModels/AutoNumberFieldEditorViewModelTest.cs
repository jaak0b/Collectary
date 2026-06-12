using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class AutoNumberFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(IReadOnlyCollection<int>? used = null)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        if (used is not null)
            ctx.LoadUsedNumbersAsync = _ => Task.FromResult(used);
        return ctx;
    }

    private static AutoNumberFieldEditorViewModel Make(
        AutoNumberFieldDefinition def, AutoNumberFieldValue value, IReadOnlyCollection<int>? used = null) =>
        new(def, value, MakeContext(used));

    [Test]
    public async Task NewItem_HighestPlusOne_ComputesMaxPlusOne()
    {
        var sut = Make(new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.HighestPlusOne },
            new AutoNumberFieldValue(), used: new[] { 1, 2, 5 });

        await sut.Ready;

        Assert.That(sut.Number, Is.EqualTo(6));
    }

    [Test]
    public async Task NewItem_FillGaps_ComputesLowestGap()
    {
        var sut = Make(new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.FillGaps },
            new AutoNumberFieldValue(), used: new[] { 1, 2, 4 });

        await sut.Ready;

        Assert.That(sut.Number, Is.EqualTo(3));
    }

    [Test]
    public async Task ExistingItem_KeepsStoredValue()
    {
        var sut = Make(new AutoNumberFieldDefinition(),
            new AutoNumberFieldValue { Value = 10 }, used: new[] { 1, 2 });

        await sut.Ready;

        Assert.That(sut.Number, Is.EqualTo(10));
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
            new AutoNumberFieldValue { Value = 1 }, used: new[] { 5 });
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
            new AutoNumberFieldValue { Value = 1 }, used: new[] { 5 });
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
            new AutoNumberFieldValue { Value = 1 }, used: new[] { 5 });
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
            new AutoNumberFieldValue { Value = 5 }, used: new[] { 5 });
        await sut.Ready;

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasDuplicate, Is.True);
            Assert.That(sut.HasNotice, Is.False);
            Assert.That(sut.Validate(), Is.Null);
        });
    }

    [Test]
    public async Task Initialize_WhenLookupFails_KeepsExistingValueAndDoesNotBlock()
    {
        var ctx = MakeContext();
        ctx.LoadUsedNumbersAsync = _ => throw new InvalidOperationException("boom");
        var sut = new AutoNumberFieldEditorViewModel(
            new AutoNumberFieldDefinition(), new AutoNumberFieldValue { Value = 3 }, ctx);

        await sut.Ready;

        Assert.Multiple(() =>
        {
            Assert.That(sut.Number, Is.EqualTo(3));
            Assert.That(sut.Validate(), Is.Null);
        });
    }

    [Test]
    public async Task ReadOnly_NewItem_WhenLookupFails_SurfacesErrorAndBlocksSave()
    {
        var ctx = MakeContext();
        ctx.LoadUsedNumbersAsync = _ => throw new InvalidOperationException("boom");
        var sut = new AutoNumberFieldEditorViewModel(
            new AutoNumberFieldDefinition { Editable = false }, new AutoNumberFieldValue(), ctx);

        await sut.Ready;

        Assert.Multiple(() =>
        {
            Assert.That(sut.Number, Is.Null);
            Assert.That(sut.HasNotice, Is.True);
            Assert.That(sut.NoticeIsError, Is.True);
            Assert.That(sut.NoticeText, Is.EqualTo(LocalizationService.Instance["AutoNumber_CouldNotAssign"]));
            Assert.That(sut.Validate(), Is.EqualTo(LocalizationService.Instance["AutoNumber_CouldNotAssign"]));
        });
    }

    [Test]
    public async Task GetCurrentValue_WritesNumberBack()
    {
        var value = new AutoNumberFieldValue();
        var sut = Make(new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.HighestPlusOne }, value, used: new[] { 7 });
        await sut.Ready;

        var result = (AutoNumberFieldValue)sut.GetCurrentValue();

        Assert.That(result.Value, Is.EqualTo(8));
    }
}
