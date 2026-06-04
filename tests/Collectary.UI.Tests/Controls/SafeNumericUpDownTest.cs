using System.Reflection;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
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
}
