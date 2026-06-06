using Avalonia;
using Avalonia.Controls;

namespace Collectary.UI.Controls;

/// <summary>
/// A settings line that shows its label beside the control on wide layouts and stacks the label
/// above a full-width control once the row is narrower than <see cref="NarrowThreshold"/>. The row
/// measures its own width, so phones reflow without any view-model plumbing.
/// </summary>
public class SettingsRow : ContentControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<SettingsRow, string?>(nameof(Label));

    public static readonly StyledProperty<double> NarrowThresholdProperty =
        AvaloniaProperty.Register<SettingsRow, double>(nameof(NarrowThreshold), 400);

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double NarrowThreshold
    {
        get => GetValue(NarrowThresholdProperty);
        set => SetValue(NarrowThresholdProperty, value);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyNarrow(e.NewSize.Width);
    }

    private void ApplyNarrow(double width) =>
        PseudoClasses.Set(":narrow", width > 0 && width < NarrowThreshold);
}
