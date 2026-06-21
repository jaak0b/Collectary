using Avalonia.Data;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class DatePickerBoxTest
{
    [Test]
    public void SelectedDate_From_And_To_AreTwoWayByDefault()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DatePickerBox.SelectedDateProperty.GetMetadata(typeof(DatePickerBox)).DefaultBindingMode,
                Is.EqualTo(BindingMode.TwoWay), "single Date field binds DateTime? straight to SelectedDate");
            Assert.That(DatePickerBox.FromProperty.GetMetadata(typeof(DatePickerBox)).DefaultBindingMode,
                Is.EqualTo(BindingMode.TwoWay), "range field binds From two-way");
            Assert.That(DatePickerBox.ToProperty.GetMetadata(typeof(DatePickerBox)).DefaultBindingMode,
                Is.EqualTo(BindingMode.TwoWay), "range field binds To two-way");
        });
    }
}
