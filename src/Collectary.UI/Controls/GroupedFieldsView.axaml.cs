using Avalonia;
using Avalonia.Controls;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Controls;

public partial class GroupedFieldsView : UserControl
{
    public static readonly StyledProperty<int> UngroupedColumnCountProperty =
        AvaloniaProperty.Register<GroupedFieldsView, int>(nameof(UngroupedColumnCount), defaultValue: 1);

    public int UngroupedColumnCount
    {
        get => GetValue(UngroupedColumnCountProperty);
        set => SetValue(UngroupedColumnCountProperty, value);
    }

    public GroupedFieldsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is IGroupedFieldHost host)
            UngroupedColumnCount = host.UngroupedColumnCount;
    }
}
