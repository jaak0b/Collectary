using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;
using FakeItEasy;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class FileAttachmentFieldEditorViewTest
{
    private static ItemEditingContext MakeContext() => new(
        editorRegistry: A.Fake<IFieldEditorRegistry>(),
        listCellBuilder: A.Fake<IListCellBuilder>(),
        goBack: () => { },
        pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
        exportImageAsync: (_, _) => Task.CompletedTask,
        loadImageBitmap: _ => null,
        deleteImageAsync: _ => Task.CompletedTask);

    private static FileAttachmentFieldEditorView Render(FileAttachmentFieldEditorViewModel vm, out Window window)
    {
        var view = new FileAttachmentFieldEditorView { DataContext = vm };
        window = new Window { Content = view, Width = 600, Height = 300 };
        window.Resources.MergedDictionaries.Add(
            (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://Collectary.UI/Controls/FieldEditorScaffold.axaml")));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return view;
    }

    [Test]
    public void RendersChipsWithEditableNameBoxes()
    {
        var value = new FileAttachmentFieldValue { Files = [new("k1", "manual.pdf"), new("k2", "warranty.pdf")] };
        var vm = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(), value, MakeContext());

        var view = Render(vm, out var window);
        try
        {
            var nameBoxes = view.GetVisualDescendants().OfType<TextBox>().ToList();
            Assert.That(nameBoxes, Has.Count.EqualTo(2));
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void EditingNameTextBox_IsEditable_ExtensionLabelIsNot()
    {
        var value = new FileAttachmentFieldValue { Files = [new("k1", "manual.pdf")] };
        var vm = new FileAttachmentFieldEditorViewModel(new FileAttachmentFieldDefinition(), value, MakeContext());

        var view = Render(vm, out var window);
        try
        {
            var nameBox = view.GetVisualDescendants().OfType<TextBox>().Single();
            Assert.That(nameBox.IsEffectivelyEnabled, Is.True);
            Assert.That(nameBox.Text, Is.EqualTo("manual"));
        }
        finally
        {
            window.Close();
        }
    }
}
