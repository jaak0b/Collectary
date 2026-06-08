#if DEBUG
using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Core.Domain;
using Collectary.Presentation.Templates.Catalog;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class DeveloperItemEditorViewTest : FlowTestBase
{
    [Test]
    public async Task DeveloperItem_RendersAndFillsRandom_DoesNotCrash()
    {
        var preset = new DeveloperTemplate().Build();
        await PresetUseCase.CreatePresetAsync(preset);
        var reloaded = (await PresetRepo.GetAllAsync()).Single(p => p.Id == preset.Id);
        var effective = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var vm = MakeItemEditorVm(reloaded, effective);
        var view = new ItemEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Assert.DoesNotThrow(() =>
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            vm.FillRandomCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            window.Width = 400;
            Dispatcher.UIThread.RunJobs();
        });
    }

    [Test]
    public async Task DeveloperItem_BesideGlobal_OpenedNarrow_RendersLabelsAbove_DoesNotCrash()
    {
        var preset = new DeveloperTemplate().Build();
        await PresetUseCase.CreatePresetAsync(preset);
        var reloaded = (await PresetRepo.GetAllAsync()).Single(p => p.Id == preset.Id);
        var effective = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var ctx = MakeItemContext();
        ctx.GlobalFieldLabelLayout = FieldLabelLayout.Beside;
        ctx.IsNarrow = true;
        var vm = new ItemEditorViewModel(
            ItemUseCase, PresetUseCase, reloaded, effective,
            onSaved: () => { }, onCancelled: () => { }, context: ctx);
        ctx.SaveAsync = vm.PersistAsync;

        Assume.That(vm.FieldEditors, Has.All.Property(nameof(FieldEditorViewModelBase.LabelAbove)).True);

        var view = new ItemEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 600, Height = 800 };

        Assert.DoesNotThrow(() =>
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.Width = 1000;
            Dispatcher.UIThread.RunJobs();
        });
    }
}
#endif
