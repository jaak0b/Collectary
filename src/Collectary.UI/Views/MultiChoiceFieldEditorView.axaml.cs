using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class MultiChoiceFieldEditorView : UserControl
{
    public MultiChoiceFieldEditorView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        CollapsedButton.Flyout = DataContext is MultiChoiceFieldEditorViewModel { IsCollapsed: true } vm
            ? BuildChoiceFlyout(vm)
            : null;
    }

    private Flyout BuildChoiceFlyout(MultiChoiceFieldEditorViewModel vm)
    {
        var panel = new StackPanel { Spacing = 4, MinWidth = 180, Margin = new Thickness(4) };
        foreach (var item in vm.ChoiceItems)
        {
            var checkBox = new CheckBox { Content = item.Label };
            checkBox.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(MultiChoiceItemViewModel.IsSelected))
            {
                Source = item,
                Mode = BindingMode.TwoWay,
            });
            panel.Children.Add(checkBox);
        }

        return new Flyout { Content = panel, Placement = PlacementMode.BottomEdgeAlignedLeft };
    }
}
