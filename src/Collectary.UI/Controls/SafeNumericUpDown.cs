using System;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace Collectary.UI.Controls;

public class SafeNumericUpDown : NumericUpDown
{
    protected override Type StyleKeyOverride => typeof(NumericUpDown);

    protected override AutomationPeer OnCreateAutomationPeer() => new ControlAutomationPeer(this);
}
