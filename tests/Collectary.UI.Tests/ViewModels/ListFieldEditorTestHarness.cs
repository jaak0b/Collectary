using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

internal static class ListFieldEditorTestHarness
{
    public static (IFieldEditorRegistry registry, IListCellBuilder cellBuilder) MakeFakes()
    {
        var registry = A.Fake<IFieldEditorRegistry>();
        var cellBuilder = A.Fake<IListCellBuilder>();

        A.CallTo(() => registry.Create(A<FieldDefinition>._, A<FieldValue?>._, A<ItemEditingContext>._))
            .ReturnsLazily((FieldDefinition def, FieldValue? _, ItemEditingContext _) =>
            {
                var editor = A.Fake<FieldEditorViewModelBase>();
                A.CallTo(() => editor.Definition).Returns(def);
                A.CallTo(() => editor.GetCurrentValue())
                    .Returns(new TextFieldValue { FieldDefinitionId = def.Id });
                return editor;
            });

        A.CallTo(() => cellBuilder.Build(A<IReadOnlyList<FieldValue>>._, A<IReadOnlyList<FieldDefinition>>._))
            .Returns(new List<ListCellViewModelBase?>());

        return (registry, cellBuilder);
    }

    public static ItemEditingContext MakeContext(
        IFieldEditorRegistry registry,
        IListCellBuilder cellBuilder,
        Action<ListFieldEditorViewModel>? openList = null,
        Action<ListEntryEditorViewModel, string>? openEntry = null,
        Func<Task>? save = null,
        Action? goBack = null)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: registry,
            listCellBuilder: cellBuilder,
            goBack: goBack ?? (() => { }),
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        if (openList is not null) ctx.OpenList = openList;
        if (openEntry is not null) ctx.OpenEntry = openEntry;
        if (save is not null) ctx.SaveAsync = save;
        return ctx;
    }

    public static ListFieldDefinition DefinitionWith(params FieldDefinition[] subFields)
    {
        var def = new ListFieldDefinition { Label = "Episodes" };
        foreach (var f in subFields) def.SubFields.Add(f);
        return def;
    }

    public static ListFieldValue ValueWithEntries(int count)
    {
        var value = new ListFieldValue();
        for (var i = 0; i < count; i++)
            value.Entries.Add(new ListEntry { ListFieldValueId = value.Id, DisplayOrder = i });
        return value;
    }
}
