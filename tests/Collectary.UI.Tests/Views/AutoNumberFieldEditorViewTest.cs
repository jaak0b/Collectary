using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Controls;
using Collectary.UI.Views;
using FakeItEasy;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class AutoNumberFieldEditorViewTest
{
    private static ItemEditingContext MakeContext(IReadOnlyCollection<int> used)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        ctx.LoadUsedNumbersAsync = _ => Task.FromResult(used);
        return ctx;
    }

    [Test]
    public async Task DuplicateNotice_DoesNotResizeTheNumberControl()
    {
        var def = new AutoNumberFieldDefinition { Editable = true, OnDuplicate = DuplicateHandling.Warn, Label = "No" };
        var vm = new AutoNumberFieldEditorViewModel(def, new AutoNumberFieldValue { FieldDefinitionId = def.Id }, MakeContext(new[] { 5 }));
        await vm.Ready;

        var view = new AutoNumberFieldEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 600, Height = 200 };
        window.Resources.MergedDictionaries.Add(
            (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://Collectary.UI/Controls/FieldEditorScaffold.axaml")));
        try
        {
            window.Show();
            vm.Number = 7;
            Dispatcher.UIThread.RunJobs();
            Assert.That(vm.HasNotice, Is.False, "precondition: a non-duplicate value shows no notice");
            var widthWithoutNotice = Spinner(view).Bounds.Width;

            vm.Number = 5;
            Dispatcher.UIThread.RunJobs();
            Assert.That(vm.HasNotice, Is.True, "precondition: a duplicate value shows the notice");
            var widthWithNotice = Spinner(view).Bounds.Width;

            Assert.That(widthWithNotice, Is.EqualTo(widthWithoutNotice).Within(0.5),
                "the duplicate notice must not stretch or shrink the number control");
        }
        finally
        {
            window.Close();
        }
    }

    private static SafeNumericUpDown Spinner(Control view) =>
        view.GetVisualDescendants().OfType<SafeNumericUpDown>().Single();
}
