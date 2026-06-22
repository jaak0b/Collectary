using System.Linq;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ClockPickerViewModelTest
{
    [Test]
    public void StartsInHourMode_WithBothRings()
    {
        var sut = new ClockPickerViewModel(14, 30);

        Assert.That(sut.Mode, Is.EqualTo(ClockMode.Hour));
        Assert.That(sut.Numbers.Count(n => !n.IsInnerRing), Is.EqualTo(12));
        Assert.That(sut.Numbers.Count(n => n.IsInnerRing), Is.EqualTo(12));
    }

    [Test]
    public void ApplyOuter_AtNinetyDegrees_PicksThree_AndAdvancesToMinute()
    {
        var sut = new ClockPickerViewModel(0, 0);

        sut.Apply(90, innerRing: false);

        Assert.That(sut.Hour, Is.EqualTo(3));
        Assert.That(sut.Mode, Is.EqualTo(ClockMode.Minute));
    }

    [Test]
    public void ApplyInner_AtNinetyDegrees_PicksFifteen()
    {
        var sut = new ClockPickerViewModel(0, 0);

        sut.Apply(90, innerRing: true);

        Assert.That(sut.Hour, Is.EqualTo(15));
    }

    [Test]
    public void ApplyOuter_AtTop_PicksTwelve()
    {
        var sut = new ClockPickerViewModel(0, 0);
        sut.Apply(0, innerRing: false);
        Assert.That(sut.Hour, Is.EqualTo(12));
    }

    [Test]
    public void ApplyInner_AtTop_PicksMidnightZero()
    {
        var sut = new ClockPickerViewModel(5, 0);
        sut.Apply(0, innerRing: true);
        Assert.That(sut.Hour, Is.EqualTo(0));
    }

    [Test]
    public void InMinuteMode_Apply_SnapsToNearestMinute()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.EditMinute();

        sut.Apply(180, innerRing: false);

        Assert.That(sut.Minute, Is.EqualTo(30));
    }

    [Test]
    public void MinuteMode_HasTwelveFiveMinuteMarks()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.EditMinute();

        Assert.That(sut.Numbers, Has.Count.EqualTo(12));
        Assert.That(sut.Numbers.Select(n => n.Label), Does.Contain("30"));
    }

    [Test]
    public void EditHour_SwitchesBackToHourMode()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.EditMinute();

        sut.EditHour();

        Assert.That(sut.Mode, Is.EqualTo(ClockMode.Hour));
    }

    [Test]
    public void Numbers_MarkTheSelectedHour()
    {
        var sut = new ClockPickerViewModel(15, 0);

        var selected = sut.Numbers.Single(n => n.IsSelected);
        Assert.That(selected.Label, Is.EqualTo("15"));
        Assert.That(selected.IsInnerRing, Is.True);
    }

    [Test]
    public void OuterThreeSitsAtNinetyDegrees()
    {
        var sut = new ClockPickerViewModel(0, 0);

        var three = sut.Numbers.Single(n => n.Label == "3" && !n.IsInnerRing);
        Assert.That(three.Angle, Is.EqualTo(90));
    }

    [Test]
    public void InnerFifteenSitsAtNinetyDegrees()
    {
        var sut = new ClockPickerViewModel(0, 0);

        var fifteen = sut.Numbers.Single(n => n.Label == "15" && n.IsInnerRing);
        Assert.That(fifteen.Angle, Is.EqualTo(90));
    }

    [Test]
    public void Preview_HighlightsPreviewedHour_WithoutCommitting()
    {
        var sut = new ClockPickerViewModel(9, 0);

        sut.Preview(90, innerRing: false);

        Assert.That(sut.Hour, Is.EqualTo(9));
        Assert.That(sut.DisplayHour, Is.EqualTo(3));
        Assert.That(sut.Numbers.Single(n => n.IsSelected).Label, Is.EqualTo("3"));
        Assert.That(sut.SelectedAngle, Is.EqualTo(90));
    }

    [Test]
    public void ClearPreview_RevertsToCommittedSelection()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.Preview(90, innerRing: false);

        sut.ClearPreview();

        Assert.That(sut.DisplayHour, Is.EqualTo(9));
        Assert.That(sut.Numbers.Single(n => n.IsSelected).Label, Is.EqualTo("9"));
    }

    [Test]
    public void Preview_InMinuteMode_TracksTheMinute()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.EditMinute();

        sut.Preview(180, innerRing: false);

        Assert.That(sut.Minute, Is.EqualTo(0));
        Assert.That(sut.DisplayMinute, Is.EqualTo(30));
    }

    [Test]
    public void SelectedPosition_ReflectsCommittedValue()
    {
        var hour = new ClockPickerViewModel(15, 0);
        Assert.That(hour.SelectedAngle, Is.EqualTo(90));
        Assert.That(hour.SelectedInner, Is.True);

        var minute = new ClockPickerViewModel(9, 30);
        minute.EditMinute();
        Assert.That(minute.SelectedAngle, Is.EqualTo(180));
        Assert.That(minute.SelectedInner, Is.False);
    }

    [Test]
    public void SelectedInner_FalseForNoon_TrueForMidnight()
    {
        Assert.That(new ClockPickerViewModel(12, 0).SelectedInner, Is.False);
        Assert.That(new ClockPickerViewModel(13, 0).SelectedInner, Is.True);
        Assert.That(new ClockPickerViewModel(0, 0).SelectedInner, Is.True);
    }

    [Test]
    public void SelectedInner_FalseInMinuteMode_EvenForAnAfternoonHour()
    {
        var sut = new ClockPickerViewModel(15, 30);
        sut.EditMinute();

        Assert.That(sut.SelectedInner, Is.False);
    }

    [Test]
    public void EditMinute_ClearsAPendingHourPreview()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.Preview(90, innerRing: false);

        sut.EditMinute();

        Assert.That(sut.DisplayHour, Is.EqualTo(9));
    }

    [Test]
    public void EditHour_ClearsAPendingMinutePreview()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.EditMinute();
        sut.Preview(180, innerRing: false);

        sut.EditHour();

        Assert.That(sut.DisplayMinute, Is.EqualTo(0));
    }

    [Test]
    public void MidnightShowsAsDoubleZeroOnTheInnerRing()
    {
        var sut = new ClockPickerViewModel(0, 0);

        var midnight = sut.Numbers.Single(n => n.IsSelected);
        Assert.That(midnight.Label, Is.EqualTo("00"));
        Assert.That(midnight.IsInnerRing, Is.True);
    }

    [Test]
    public void PickingAnHour_RebuildsNumbersIntoTheMinuteRing()
    {
        var sut = new ClockPickerViewModel(0, 0);

        sut.Apply(90, innerRing: false);

        Assert.That(sut.Numbers, Has.Count.EqualTo(12));
    }

    [Test]
    public void EditHour_RebuildsNumbersIntoTheHourRings()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.EditMinute();

        sut.EditHour();

        Assert.That(sut.Numbers, Has.Count.EqualTo(24));
    }

    [Test]
    public void MinuteMarks_ArePaddedToTwoDigits()
    {
        var sut = new ClockPickerViewModel(9, 0);
        sut.EditMinute();

        Assert.That(sut.Numbers.Select(n => n.Label), Does.Contain("05"));
    }

    [Test]
    public void MinuteThirtySitsAtOneEightyDegrees()
    {
        var sut = new ClockPickerViewModel(9, 30);
        sut.EditMinute();

        var thirty = sut.Numbers.Single(n => n.Label == "30");
        Assert.That(thirty.Angle, Is.EqualTo(180));
        Assert.That(thirty.IsSelected, Is.True);
    }

    [TestCase(90, false, 3)]
    [TestCase(90, true, 15)]
    [TestCase(0, false, 12)]
    [TestCase(0, true, 0)]
    [TestCase(330, false, 11)]
    public void HourAt_MapsAngleAndRingToHour(double angle, bool inner, int expected)
    {
        var sut = new ClockPickerViewModel(0, 0);
        Assert.That(sut.HourAt(angle, inner), Is.EqualTo(expected));
    }

    [TestCase(0, 0)]
    [TestCase(180, 30)]
    [TestCase(210, 35)]
    public void MinuteAt_MapsAngleToMinute(double angle, int expected)
    {
        var sut = new ClockPickerViewModel(0, 0);
        Assert.That(sut.MinuteAt(angle), Is.EqualTo(expected));
    }

    [Test]
    public void MinuteMarks_AreAllOnTheOuterRing()
    {
        var sut = new ClockPickerViewModel(9, 30);
        sut.EditMinute();

        Assert.That(sut.Numbers.All(n => !n.IsInnerRing), Is.True);
    }

    [Test]
    public void MinuteMode_MarksExactlyTheSelectedMinute()
    {
        var sut = new ClockPickerViewModel(9, 30);
        sut.EditMinute();

        Assert.That(sut.Numbers.Count(n => n.IsSelected), Is.EqualTo(1));
    }
}
