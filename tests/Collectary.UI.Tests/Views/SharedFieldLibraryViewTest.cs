using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels.SharedFields;
using Collectary.UI.Views.SharedFields;
using FakeItEasy;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class SharedFieldLibraryViewTest
{
    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private static SharedFieldLibraryViewModel CreateViewModel() =>
        new(A.Fake<ISharedFieldUseCase>(), A.Fake<IDialogService>(),
            new TestFieldEditorMapper().Create(), onDone: () => { });

    private static IReadOnlyList<MenuItem> AddFieldMenuItems(SharedFieldLibraryView view)
    {
        var button = view.GetLogicalDescendants().OfType<Button>().First(b => b.Name == "AddFieldButton");
        var flyout = (MenuFlyout)button.Flyout!;
        return flyout.ItemsSource!.Cast<object>().OfType<MenuItem>().ToList();
    }

    [Test]
    public void AddFieldMenu_ListsEveryCatalogType()
    {
        var vm = CreateViewModel();
        var view = new SharedFieldLibraryView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        var headers = AddFieldMenuItems(view).Select(m => m.Header?.ToString() ?? "").ToList();

        Assert.Multiple(() =>
        {
            foreach (var entry in vm.AddableFieldTypes)
                Assert.That(headers, Has.Some.Contains(entry.Name), $"Menu missing field type '{entry.Name}'");
        });
    }

    [Test]
    public void AddFieldMenu_IncludesPreviouslyMissingTypes()
    {
        var vm = CreateViewModel();
        var view = new SharedFieldLibraryView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        var headers = AddFieldMenuItems(view).Select(m => m.Header?.ToString() ?? "").ToList();

        Assert.Multiple(() =>
        {
            foreach (var name in new[] { "Rich Text", "Percentage", "Currency", "Time", "Duration", "Phone", "Email", "Tags" })
                Assert.That(headers, Has.Some.Contains(name), $"System Fields menu should now offer '{name}'");
        });
    }
}
