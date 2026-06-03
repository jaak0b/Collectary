using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Collectary.UI.Controls;

public partial class GroupDetailEditor : UserControl
{
    public static readonly StyledProperty<ICommand?> DrillCommandProperty =
        AvaloniaProperty.Register<GroupDetailEditor, ICommand?>(nameof(DrillCommand));

    public ICommand? DrillCommand
    {
        get => GetValue(DrillCommandProperty);
        set => SetValue(DrillCommandProperty, value);
    }

    public GroupDetailEditor()
    {
        InitializeComponent();
    }
}
