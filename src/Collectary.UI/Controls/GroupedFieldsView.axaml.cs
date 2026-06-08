using Avalonia;
using Avalonia.Controls;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Controls;

public partial class GroupedFieldsView : UserControl
{
    public static readonly StyledProperty<int> UngroupedColumnCountProperty =
        AvaloniaProperty.Register<GroupedFieldsView, int>(nameof(UngroupedColumnCount), defaultValue: 1);

    public static readonly StyledProperty<double> FieldMinColumnWidthProperty =
        AvaloniaProperty.Register<GroupedFieldsView, double>(nameof(FieldMinColumnWidth), defaultValue: 200);

    public int UngroupedColumnCount
    {
        get => GetValue(UngroupedColumnCountProperty);
        set => SetValue(UngroupedColumnCountProperty, value);
    }

    public double FieldMinColumnWidth
    {
        get => GetValue(FieldMinColumnWidthProperty);
        set => SetValue(FieldMinColumnWidthProperty, value);
    }

    public GroupedFieldsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is IGroupedFieldHost host)
        {
            UngroupedColumnCount = host.UngroupedColumnCount;
            FieldMinColumnWidth = host.FieldMinColumnWidth;
        }
    }
}
