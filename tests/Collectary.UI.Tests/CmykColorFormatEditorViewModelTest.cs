using Avalonia.Media;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests;

[TestFixture]
public class CmykColorFormatEditorViewModelTest
{
    [Test]
    public void Constructor_ParsesEachComponent()
    {
        var vm = new CmykColorFormatEditorViewModel("10,20,30,40");

        Assert.That(vm.C, Is.EqualTo(10));
        Assert.That(vm.M, Is.EqualTo(20));
        Assert.That(vm.Y, Is.EqualTo(30));
        Assert.That(vm.K, Is.EqualTo(40));
    }

    [Test]
    public void NoPicker_ClampsAndEncodes()
    {
        var vm = new CmykColorFormatEditorViewModel("0,0,0,0") { C = 50, M = 200, Y = 25, K = -5 };

        Assert.That(vm.SupportsPicker, Is.False);
        Assert.That(vm.C, Is.EqualTo(50));
        Assert.That(vm.M, Is.EqualTo(100));
        Assert.That(vm.Y, Is.EqualTo(25));
        Assert.That(vm.K, Is.EqualTo(0));
        Assert.That(vm.Encode(), Is.EqualTo("50,100,25,0"));
        Assert.That(vm.SwatchBrush, Is.TypeOf<SolidColorBrush>());
    }

    [Test]
    public void SettingComponent_RaisesSwatchBrushNotification()
    {
        var vm = new CmykColorFormatEditorViewModel("0,0,0,0");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.C = 60;

        Assert.That(raised, Does.Contain(nameof(vm.SwatchBrush)));
    }

    [Test]
    public void SettingComponent_ToSameValue_DoesNotRaiseNotification()
    {
        var vm = new CmykColorFormatEditorViewModel("30,0,0,0");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.C = 30;

        Assert.That(raised, Is.Empty);
    }

    [Test]
    public void SwatchBrush_ReflectsCmykToColor()
    {
        var vm = new CmykColorFormatEditorViewModel("0,0,0,0");
        var brush = (SolidColorBrush)vm.SwatchBrush;
        Assert.That(brush.Color, Is.EqualTo(Colors.White));

        vm.K = 100;
        var black = (SolidColorBrush)vm.SwatchBrush;
        Assert.That(black.Color, Is.EqualTo(Color.FromRgb(0, 0, 0)));
    }

    [Test]
    public void Encode_ClampsAllComponents()
    {
        var vm = new CmykColorFormatEditorViewModel("0,0,0,0") { C = -1, M = 101, Y = 50, K = 100 };
        Assert.That(vm.Encode(), Is.EqualTo("0,100,50,100"));
    }
}
