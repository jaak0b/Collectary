#if DEBUG
using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.Templates.Catalog;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;
using FakeItEasy;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class DeveloperTemplateViewTest
{
    [Test]
    public void SeedDeveloperTemplate_RendersPresetEditor_DoesNotCrash()
    {
        var presetUseCase = A.Fake<IPresetUseCase>();
        var sharedFieldUseCase = A.Fake<ISharedFieldUseCase>();
        var dialogService = A.Fake<IDialogService>();
        var mapper = new TestFieldEditorMapper().Create();
        var seed = new DeveloperTemplate().Build();

        var vm = new PresetEditorViewModel(presetUseCase, sharedFieldUseCase, dialogService, mapper,
            onSaved: () => { }, onCancelled: () => { }, seed: seed);

        var view = new PresetEditorView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 700 };

        Assert.DoesNotThrow(() =>
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            _ = vm.LoadAsync();
            Dispatcher.UIThread.RunJobs();

            var group = vm.CurrentRows.OfType<FieldGroupRowViewModel>().FirstOrDefault();
            if (group is not null)
            {
                vm.DrillIntoCommand.Execute(group);
                Dispatcher.UIThread.RunJobs();
            }

            window.Width = 400;
            Dispatcher.UIThread.RunJobs();
        });
    }
}
#endif
