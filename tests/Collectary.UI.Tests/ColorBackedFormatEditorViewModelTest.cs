using Avalonia.Media;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests;

[TestFixture]
public class ColorBackedFormatEditorViewModelTest
{
    [Test]
    public void PickerColorRoundTrips()
    {
        var vm = new HexColorFormatEditorViewModel("#102030") { PickerColor = Colors.Red };
        Assert.That(vm.Encode(), Is.EqualTo("#FF0000"));
        Assert.That(vm.SwatchBrush, Is.TypeOf<SolidColorBrush>());
    }

    [Test]
    public void PickerColor_GetterReturnsCurrent()
    {
        var vm = new ArgbColorFormatEditorViewModel("255,1,2,3");
        Assert.That(vm.PickerColor, Is.EqualTo(Color.FromArgb(255, 1, 2, 3)));
    }

    [Test]
    public void SwatchBrush_ReflectsCurrentColor()
    {
        var vm = new HexColorFormatEditorViewModel("#112233");
        var brush = (SolidColorBrush)vm.SwatchBrush;
        Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(0x11, 0x22, 0x33)));
    }

    [Test]
    public void SettingPickerColor_RaisesNotifications()
    {
        var vm = new HexColorFormatEditorViewModel("#000000");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.PickerColor = Colors.Red;

        Assert.That(raised, Does.Contain(nameof(vm.PickerColor)));
        Assert.That(raised, Does.Contain(nameof(vm.SwatchBrush)));
    }

    [Test]
    public void SettingPickerColor_ToSameValue_DoesNotRaise()
    {
        var vm = new HexColorFormatEditorViewModel("#FF0000");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.PickerColor = Colors.Red;

        Assert.That(raised, Is.Empty);
    }

    [Test]
    public void SupportsPicker_IsTrue()
    {
        Assert.That(new HexColorFormatEditorViewModel("#000000").SupportsPicker, Is.True);
    }
}
