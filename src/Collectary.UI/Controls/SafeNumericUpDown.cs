using System;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Collectary.UI.Controls;

public class SafeNumericUpDown : NumericUpDown
{
    private ButtonSpinner? _spinner;

    protected override Type StyleKeyOverride => typeof(NumericUpDown);

    protected override AutomationPeer OnCreateAutomationPeer() => new ControlAutomationPeer(this);

    // Subscribe before base.OnApplyTemplate so our spin handler runs first, while Value is still null —
    // OnSpinFromEmpty explains why that ordering is load-bearing.
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_spinner is not null) _spinner.Spin -= OnSpinFromEmpty;
        _spinner = e.NameScope.Find<ButtonSpinner>("PART_Spinner");
        if (_spinner is not null) _spinner.Spin += OnSpinFromEmpty;
        base.OnApplyTemplate(e);
    }

    // On a null value the base NumericUpDown steps from the opposite bound (Minimum on increase, Maximum
    // on decrease) — the full int range for an unbounded field, so an empty box jumps to an int extreme.
    // Step from a 0 baseline instead, clamped, and mark the event handled so the base handler (registered
    // after ours) doesn't re-run. This must precede the base handler: once base writes a value, it is no
    // longer null and this guard would skip it.
    private void OnSpinFromEmpty(object? sender, SpinEventArgs e)
    {
        if (Value is not null || !AllowSpin || IsReadOnly || Minimum > Maximum) return;
        var delta = e.Direction == SpinDirection.Increase ? Increment : -Increment;
        SetCurrentValue(ValueProperty, Math.Clamp(0m + delta, Minimum, Maximum));
        e.Handled = true;
    }
}
