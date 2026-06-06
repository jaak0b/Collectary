using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class DialogViewsTest
{
    private static T Render<T>(T view) where T : Control
    {
        var window = new Window { Content = view, Width = 600, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return view;
    }

    [Test]
    public void MessageDialogView_OkButton_CompletesDialog()
    {
        var vm = new MessageDialogViewModel("Saved failed", "Oops");
        var view = Render(new MessageDialogView { DataContext = vm });

        var ok = view.GetVisualDescendants().OfType<Button>().Single();
        ok.Command!.Execute(ok.CommandParameter);

        Assert.That(vm.Completion.IsCompletedSuccessfully, Is.True);
        Assert.That(vm.Completion.Result, Is.Null);
    }

    [Test]
    public void ConfirmDialogView_ConfirmButton_CompletesWithTrue()
    {
        var vm = new ConfirmDialogViewModel("Delete?", "Delete", "Cancel", "Confirm");
        var view = Render(new ConfirmDialogView { DataContext = vm });

        var confirm = view.GetVisualDescendants().OfType<Button>()
            .Single(b => ReferenceEquals(b.Command, vm.ConfirmCommand));
        confirm.Command!.Execute(null);

        Assert.That(vm.Completion.IsCompletedSuccessfully, Is.True);
        Assert.That(vm.Completion.Result, Is.EqualTo(true));
    }

    [Test]
    public void ConfirmDialogView_CancelButton_CompletesWithFalse()
    {
        var vm = new ConfirmDialogViewModel("Delete?", "Delete", "Cancel", "Confirm");
        var view = Render(new ConfirmDialogView { DataContext = vm });

        var cancel = view.GetVisualDescendants().OfType<Button>()
            .Single(b => ReferenceEquals(b.Command, vm.CancelCommand));
        cancel.Command!.Execute(null);

        Assert.That(vm.Completion.IsCompletedSuccessfully, Is.True);
        Assert.That(vm.Completion.Result, Is.EqualTo(false));
    }
}
