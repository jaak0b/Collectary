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
        var systemFieldUseCase = A.Fake<ISystemFieldUseCase>();
        var dialogService = A.Fake<IDialogService>();
        return new PresetEditorViewModel(presetUseCase, systemFieldUseCase, dialogService,
            onSaved: () => { }, onCancelled: () => { });
    }

    private static StackPanel FindPanel(PresetEditorView view) =>
        view.GetLogicalDescendants().OfType<StackPanel>()
            .First(p => p.Name == "PresetColumnCountPanel");

    [Test]
    public void PresetColumnCountControl_HiddenWhenDrilledIntoGroup()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        var panel = FindPanel(view);
        Assert.That(panel.IsVisible, Is.True, "Preset Columns control is shown at the root level");

        vm.AddGroupCommand.Execute(null);
        var group = vm.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        vm.DrillIntoCommand.Execute(group);
        Dispatcher.UIThread.RunJobs();

        Assert.That(panel.IsVisible, Is.False,
            "Preset Columns control must be hidden while drilled into a group, so it can't be edited by mistake");
    }

    [Test]
    public void PresetColumnCountControl_VisibleAgainAfterNavigatingBackToRoot()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        vm.AddGroupCommand.Execute(null);
        var group = vm.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        vm.DrillIntoCommand.Execute(group);
        vm.NavigateToLevelCommand.Execute(vm.Levels[0]);
        Dispatcher.UIThread.RunJobs();

        Assert.That(FindPanel(view).IsVisible, Is.True);
    }
}
