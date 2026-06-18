using System.Linq;
using System.Reflection;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class SafeNumericUpDownTest
{
    [Test]
    public void StyleKeyOverride_IsBaseNumericUpDown_SoTheTemplateApplies()
    {
        var prop = typeof(SafeNumericUpDown)
            .GetProperty("StyleKeyOverride", BindingFlags.NonPublic | BindingFlags.Instance);
        var key = prop!.GetValue(new SafeNumericUpDown());

        Assert.That(key, Is.EqualTo(typeof(NumericUpDown)),
            "SafeNumericUpDown must reuse the NumericUpDown control theme or it renders invisible");
    }

    [Test]
    public void AutomationPeer_IsPlainControlPeer_AvoidingDecimalUiaCrash()
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(new SafeNumericUpDown());

        Assert.That(peer, Is.TypeOf<ControlAutomationPeer>(),
            "Must not use NumericUpDownAutomationPeer, whose decimal property-changed event crashes Win32 UIA");
    }

    [Test]
    public void Spin_FromEmpty_StepsUpFromZero_NotToTheMaximum()
    {
        var (control, window) = ShowSpinner(value: null, minimum: int.MinValue, maximum: int.MaxValue);
        try
        {
            Spin(control, SpinDirection.Increase);
            Assert.That(control.Value, Is.EqualTo(1m),
                "an empty field stepped up must start from 0 (=> 1), not jump to an int extreme");
        }
        finally { window.Close(); }
    }

    [Test]
    public void Spin_FromEmpty_StepsDownFromZero_NotToTheMinimum()
    {
        var (control, window) = ShowSpinner(value: null, minimum: int.MinValue, maximum: int.MaxValue);
        try
        {
            Spin(control, SpinDirection.Decrease);
            Assert.That(control.Value, Is.EqualTo(-1m),
                "an empty field stepped down must start from 0 (=> -1), not jump to an int extreme");
        }
        finally { window.Close(); }
    }

    [Test]
    public void Spin_FromExistingValue_StepsNormally()
    {
        var (control, window) = ShowSpinner(value: 5m, minimum: int.MinValue, maximum: int.MaxValue);
        try
        {
            Spin(control, SpinDirection.Increase);
            Assert.That(control.Value, Is.EqualTo(6m), "a populated value must still step by the increment");

            Spin(control, SpinDirection.Decrease);
            Assert.That(control.Value, Is.EqualTo(5m), "stepping down returns it");
        }
        finally { window.Close(); }
    }

    [Test]
    public void Spin_UpFromEmpty_ClampsToConfiguredMinimum()
    {
        var (control, window) = ShowSpinner(value: null, minimum: 10m, maximum: 100m);
        try
        {
            Spin(control, SpinDirection.Increase);
            Assert.That(control.Value, Is.EqualTo(10m),
                "stepping up from 0 must clamp up into the configured [Min, Max] range");
        }
        finally { window.Close(); }
    }

    [Test]
    public void Spin_DownFromEmpty_ClampsToConfiguredMinimum()
    {
        var (control, window) = ShowSpinner(value: null, minimum: 10m, maximum: 100m);
        try
        {
            Spin(control, SpinDirection.Decrease);
            Assert.That(control.Value, Is.EqualTo(10m),
                "stepping down from 0 must clamp up to Minimum, never settle below the configured range");
        }
        finally { window.Close(); }
    }

    [Test]
    public void Spin_UpFromEmpty_ClampsToConfiguredMaximum_WhenRangeIsNegative()
    {
        var (control, window) = ShowSpinner(value: null, minimum: -100m, maximum: -10m);
        try
        {
            Spin(control, SpinDirection.Increase);
            Assert.That(control.Value, Is.EqualTo(-10m),
                "stepping up from 0 must clamp down to Maximum, never settle above the configured range");
        }
        finally { window.Close(); }
    }

    private static (SafeNumericUpDown Control, Window Window) ShowSpinner(
        decimal? value, decimal minimum, decimal maximum)
    {
        var control = new SafeNumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Increment = 1,
            Value = value,
        };
        var window = new Window { Content = control, Width = 200, Height = 80 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (control, window);
    }

    private static void Spin(SafeNumericUpDown control, SpinDirection direction)
    {
        var spinner = control.GetVisualDescendants().OfType<ButtonSpinner>().Single();
        spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, direction));
        Dispatcher.UIThread.RunJobs();
    }
}
