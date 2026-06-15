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

    private static Control FindNamed(PresetEditorView view, string name) =>
        view.GetLogicalDescendants().OfType<Control>().First(c => c.Name == name);

    [Test]
    public void CollectionSettingsHeader_HidesLabelPositionWhenNarrow()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var label = FindNamed(view, "LabelLayoutField");
        Assert.That(label.IsVisible, Is.True, "wide: the label-position picker is shown");

        window.Width = 400;
        Dispatcher.UIThread.RunJobs();

        Assert.That(label.IsVisible, Is.False,
            "narrow: labels always fold above their inputs, so the label-position picker is hidden");
    }

    [Test]
    public void CollectionSettingsHeader_WarningKeepsItsOwnFullWidthRowInBothLayouts()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var warning = FindNamed(view, "NameWarningText");

        Assert.Multiple(() =>
        {
            Assert.That(Grid.GetRow(warning), Is.EqualTo(1), "wide: warning is its own row beneath the controls");
            Assert.That(Grid.GetColumn(warning), Is.EqualTo(0));
            Assert.That(Grid.GetColumnSpan(warning), Is.EqualTo(4), "wide: warning spans all columns so it never wraps");
        });

        window.Width = 400;
        Dispatcher.UIThread.RunJobs();

        Assert.Multiple(() =>
        {
            Assert.That(Grid.GetRow(warning), Is.EqualTo(2), "narrow: warning is the last row, under name and the controls");
            Assert.That(Grid.GetColumn(warning), Is.EqualTo(0));
            Assert.That(Grid.GetColumnSpan(warning), Is.EqualTo(2), "narrow: warning spans both columns (full width)");
        });
    }

    [Test]
    public void CollectionSettingsHeader_ReflowsControlsByWidth()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var name = FindNamed(view, "NameGroup");
        var parent = FindNamed(view, "ParentField");
        var stepper = FindNamed(view, "ColumnStepper");

        Assert.Multiple(() =>
        {
            Assert.That(Grid.GetRow(name), Is.EqualTo(0), "wide: name on the single control row");
            Assert.That(Grid.GetColumn(name), Is.EqualTo(0));
            Assert.That(Grid.GetRow(parent), Is.EqualTo(0), "wide: parent on the single control row");
            Assert.That(Grid.GetColumn(parent), Is.EqualTo(1));
            Assert.That(Grid.GetRow(stepper), Is.EqualTo(0), "wide: column count on the single control row");
            Assert.That(Grid.GetColumn(stepper), Is.EqualTo(2));
        });

        window.Width = 400;
        Dispatcher.UIThread.RunJobs();

        Assert.Multiple(() =>
        {
            Assert.That(Grid.GetRow(name), Is.EqualTo(0), "narrow row 1: name");
            Assert.That(Grid.GetColumnSpan(name), Is.EqualTo(2), "narrow: name fills the full width");
            Assert.That(Grid.GetRow(stepper), Is.EqualTo(1), "narrow row 2: column count first");
            Assert.That(Grid.GetColumn(stepper), Is.EqualTo(0));
            Assert.That(Grid.GetRow(parent), Is.EqualTo(1), "narrow row 2: then parent preset");
            Assert.That(Grid.GetColumn(parent), Is.EqualTo(1), "narrow: parent fills the remaining space");
        });
    }

    private static Button FindBack(PresetEditorView view) =>
        view.GetLogicalDescendants().OfType<Button>()
            .First(b => b.Name == "BackButton");

    [Test]
    public void BackButton_IsAlwaysVisibleAndBoundToBackCommand()
    {
        var vm = CreateViewModel();
        var view = new PresetEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var back = FindBack(view);
        Assert.That(back.IsVisible, Is.True, "Back sits in the footer and is always available (wide)");
        Assert.That(back.Command, Is.SameAs(vm.BackCommand));

        vm.IsNarrow = true;
        Dispatcher.UIThread.RunJobs();
        Assert.That(FindBack(view).IsVisible, Is.True, "Back stays available on a narrow screen too");
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
