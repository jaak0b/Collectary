using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Collectary.UI.Controls;

public partial class FieldDetailEditor : UserControl
{
    public static readonly StyledProperty<ICommand?> DrillCommandProperty =
        AvaloniaProperty.Register<FieldDetailEditor, ICommand?>(nameof(DrillCommand));

    public ICommand? DrillCommand
    {
        get => GetValue(DrillCommandProperty);
        set => SetValue(DrillCommandProperty, value);
    }

    public FieldDetailEditor()
    {
        InitializeComponent();
    }
}
