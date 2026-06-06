using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;
using FakeItEasy;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class PresetEditorViewTest
{
    private static PresetEditorViewModel CreateViewModel()
    {
        var presetUseCase = A.Fake<IPresetUseCase>();
        var sharedFieldUseCase = A.Fake<ISharedFieldUseCase>();
        var dialogService = A.Fake<IDialogService>();
        var mapper = new TestFieldEditorMapper().Create();
        return new PresetEditorViewModel(presetUseCase, sharedFieldUseCase, dialogService, mapper,
            onSaved: () => { }, onCancelled: () => { });
    }

    private static Grid FindHeader(PresetEditorView view) =>
        view.GetLogicalDescendants().OfType<Grid>()
            .First(g => g.Name == "CollectionSettingsHeader");

    [Test]
    public void RapidResizeAcrossNarrowThreshold_DoesNotThrow()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotThrow(() =>
        {
            for (var i = 0; i < 60; i++)
            {
                window.Width = i % 2 == 0 ? 400 : 1000;
                Dispatcher.UIThread.RunJobs();
            }
        });
    }

    [Test]
    public void CollectionSettingsHeader_HiddenWhenDrilledIntoGroup()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        var header = FindHeader(view);
        Assert.That(header.IsVisible, Is.True, "Header is shown at the root level");

        vm.AddGroupCommand.Execute(null);
        var group = vm.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        vm.DrillIntoCommand.Execute(group);
        Dispatcher.UIThread.RunJobs();

        Assert.That(header.IsVisible, Is.False,
            "Header must be hidden while drilled into a group");
    }

    [Test]
    public void CollectionSettingsHeader_VisibleAgainAfterNavigatingBackToRoot()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        vm.AddGroupCommand.Execute(null);
        var group = vm.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        vm.DrillIntoCommand.Execute(group);
        vm.NavigateToLevelCommand.Execute(vm.Levels[0]);
        Dispatcher.UIThread.RunJobs();

        Assert.That(FindHeader(view).IsVisible, Is.True);
    }
}
