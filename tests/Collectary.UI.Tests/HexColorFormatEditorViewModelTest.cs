using Avalonia.Media;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests;

[TestFixture]
public class HexColorFormatEditorViewModelTest
{
    [Test]
    public void EncodesAndReflectsSetter()
    {
        var vm = new HexColorFormatEditorViewModel("#FF0000");
        Assert.That(vm.Hex, Is.EqualTo("#FF0000"));
        Assert.That(vm.SupportsPicker, Is.True);
        Assert.That(vm.Encode(), Is.EqualTo("#FF0000"));

        vm.Hex = "#00FF00";
        Assert.That(vm.Encode(), Is.EqualTo("#00FF00"));
    }

    [Test]
    public void Hex_GetterFormatsUppercaseWithHash()
    {
        var vm = new HexColorFormatEditorViewModel("#0a0b0c");
        Assert.That(vm.Hex, Is.EqualTo("#0A0B0C"));
    }

    [Test]
    public void IgnoresInvalidSetterValue()
    {
        var vm = new HexColorFormatEditorViewModel("#FF0000");
        vm.Hex = "garbage";
        Assert.That(vm.Hex, Is.EqualTo("#FF0000"));
    }

    [Test]
    public void SettingHex_RaisesHexAndSwatchNotifications()
    {
        var vm = new HexColorFormatEditorViewModel("#FF0000");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.Hex = "#0000FF";

        Assert.That(raised, Does.Contain(nameof(vm.Hex)));
        Assert.That(raised, Does.Contain(nameof(vm.PickerColor)));
        Assert.That(raised, Does.Contain(nameof(vm.SwatchBrush)));
    }

    [Test]
    public void SettingHex_DropsAlphaKeepsRgb()
    {
        var vm = new HexColorFormatEditorViewModel("#FFFFFF");
        vm.Hex = "#80AABBCC";
        Assert.That(vm.Hex, Is.EqualTo("#AABBCC"));
    }
}
