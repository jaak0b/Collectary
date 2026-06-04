using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Collectary.UI.Controls;

/// <summary>
/// Wraps a single field editor input with its label. The label sits beside the input
/// (compact, shared label width) or above it, driven by <see cref="LabelAbove"/>.
/// </summary>
public class FieldEditorScaffold : ContentControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<FieldEditorScaffold, string?>(nameof(Label));

    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<FieldEditorScaffold, bool>(nameof(IsRequired));

    public static readonly StyledProperty<bool> LabelAboveProperty =
        AvaloniaProperty.Register<FieldEditorScaffold, bool>(nameof(LabelAbove));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsRequired
    {
        get => GetValue(IsRequiredProperty);
        set => SetValue(IsRequiredProperty, value);
    }

    public bool LabelAbove
    {
        get => GetValue(LabelAboveProperty);
        set => SetValue(LabelAboveProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LabelAboveProperty)
            PseudoClasses.Set(":above", LabelAbove);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        PseudoClasses.Set(":above", LabelAbove);
    }
}
