using Avalonia.Controls;
using Avalonia.Interactivity;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class LinkedItemFieldEditorView : UserControl
{
    public LinkedItemFieldEditorView()
    {
        InitializeComponent();
    }

    private void OnDropDownOpened(object? sender, EventArgs e)
    {
        if (DataContext is LinkedItemFieldEditorViewModel vm && vm.LoadCandidatesCommand.CanExecute(null))
            vm.LoadCandidatesCommand.Execute(null);
    }
}
