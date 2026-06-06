using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class DateBoxTest
{
    [Test]
    public void SelectedDate_IsTwoWayByDefault()
    {
        var mode = DateBox.SelectedDateProperty.GetMetadata(typeof(DateBox)).DefaultBindingMode;

        Assert.That(mode, Is.EqualTo(BindingMode.TwoWay),
            "The editor binds its DateTime? straight to SelectedDate, so it must push back two-way");
    }

    [Test]
    public void SettingSelectedDate_SyncsTheCalendar()
    {
        var box = new DateBox { SelectedDate = new DateTime(2025, 7, 4) };

        Assert.That(Calendar(box).SelectedDate, Is.EqualTo(new DateTime(2025, 7, 4)));
    }

    [Test]
    public void PickingInCalendar_UpdatesSelectedDateAndClosesPopup()
    {
        var box = new DateBox();
        var popup = Popup(box);
        popup.IsOpen = true;

        Calendar(box).SelectedDate = new DateTime(2030, 1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(box.SelectedDate, Is.EqualTo(new DateTime(2030, 1, 2)));
            Assert.That(popup.IsOpen, Is.False);
        });
    }

    private static Calendar Calendar(DateBox box) => Part<Calendar>(box, "PART_Calendar");

    private static Popup Popup(DateBox box) => Part<Popup>(box, "PART_Popup");

    private static T Part<T>(DateBox box, string name) =>
        (T)typeof(DateBox).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(box)!;
}
