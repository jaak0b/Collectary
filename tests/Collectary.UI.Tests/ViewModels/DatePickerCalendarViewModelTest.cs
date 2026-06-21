using System.Globalization;
using System.Linq;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DatePickerCalendarViewModelTest
{
    private CultureInfo _original = null!;

    [SetUp]
    public void PinCulture()
    {
        _original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
    }

    [TearDown]
    public void RestoreCulture() => CultureInfo.CurrentCulture = _original;

    private static DatePickerCalendarViewModel Range(DateTime visible, DateTime? from = null, DateTime? to = null) =>
        new(rangeMode: true, visibleMonth: visible, from: from, to: to);

    private static DatePickerCalendarViewModel Single(DateTime visible, DateTime? selected = null) =>
        new(rangeMode: false, visibleMonth: visible, from: selected, to: null);

    private static DatePickerDayViewModel Day(DatePickerCalendarViewModel vm, DateTime d) =>
        vm.Days.Single(x => x.Date.Date == d.Date);

    [Test]
    public void Days_AreA42CellGrid_WithCurrentMonthFlagged()
    {
        var vm = Range(new DateTime(2026, 6, 15));

        Assert.That(vm.Days, Has.Count.EqualTo(42));
        Assert.That(vm.Days.Count(d => d.IsCurrentMonth), Is.EqualTo(30));
    }

    [Test]
    public void FirstSelect_SetsFrom_ClearsTo_AndBlacksOutEarlierDays()
    {
        var vm = Range(new DateTime(2026, 6, 1));

        vm.SelectDay(new DateTime(2026, 6, 10));

        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 10)));
        Assert.That(vm.To, Is.Null);
        Assert.That(Day(vm, new DateTime(2026, 6, 5)).IsBlackedOut, Is.True);
        Assert.That(Day(vm, new DateTime(2026, 6, 5)).IsSelectable, Is.False);
        Assert.That(Day(vm, new DateTime(2026, 6, 20)).IsSelectable, Is.True);
        Assert.That(Day(vm, new DateTime(2026, 6, 10)).IsStart, Is.True);
    }

    [Test]
    public void HoverWhilePickingEnd_PreviewsRange_WithoutChangingSelection()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10));

        vm.HoverDay(new DateTime(2026, 6, 14));

        Assert.That(vm.To, Is.Null);
        Assert.That(Day(vm, new DateTime(2026, 6, 12)).IsPreview, Is.True);
        Assert.That(Day(vm, new DateTime(2026, 6, 14)).IsPreview, Is.True);
        Assert.That(Day(vm, new DateTime(2026, 6, 16)).IsPreview, Is.False);
    }

    [Test]
    public void ClearHover_RemovesPreview()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10));
        vm.HoverDay(new DateTime(2026, 6, 14));

        vm.ClearHover();

        Assert.That(Day(vm, new DateTime(2026, 6, 12)).IsPreview, Is.False);
    }

    [Test]
    public void SecondSelect_AfterStart_SetsToAndMarksRange()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10));

        vm.SelectDay(new DateTime(2026, 6, 14));

        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 10)));
        Assert.That(vm.To, Is.EqualTo(new DateTime(2026, 6, 14)));
        Assert.That(Day(vm, new DateTime(2026, 6, 12)).IsInRange, Is.True);
        Assert.That(Day(vm, new DateTime(2026, 6, 14)).IsEnd, Is.True);
    }

    [Test]
    public void SelectingEarlierDayWhilePickingEnd_RestartsTheStart()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10));

        vm.SelectDay(new DateTime(2026, 6, 4));

        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 4)));
        Assert.That(vm.To, Is.Null);
    }

    [Test]
    public void SelectingWhenRangeComplete_StartsOver()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10), to: new DateTime(2026, 6, 14));

        vm.SelectDay(new DateTime(2026, 6, 20));

        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 20)));
        Assert.That(vm.To, Is.Null);
    }

    [Test]
    public void NextMonthThenPrevMonth_ChangesVisibleMonth_AndKeepsSelection()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10), to: new DateTime(2026, 6, 14));

        vm.NextMonth();
        Assert.That(vm.Days.First(d => d.IsCurrentMonth).Date.Month, Is.EqualTo(7));

        vm.PrevMonth();
        Assert.That(vm.Days.First(d => d.IsCurrentMonth).Date.Month, Is.EqualTo(6));
        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 10)));
        Assert.That(vm.To, Is.EqualTo(new DateTime(2026, 6, 14)));
    }

    [Test]
    public void Clear_NullsEverything()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10), to: new DateTime(2026, 6, 14));

        vm.Clear();

        Assert.That(vm.From, Is.Null);
        Assert.That(vm.To, Is.Null);
        Assert.That(vm.Days.Any(d => d.IsInRange || d.IsStart || d.IsEnd), Is.False);
    }

    [Test]
    public void SingleMode_SelectDay_SetsOneDate_NoToNoBlackoutNoPreview()
    {
        var vm = Single(new DateTime(2026, 6, 1));

        vm.SelectDay(new DateTime(2026, 6, 10));

        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 10)));
        Assert.That(vm.To, Is.Null);
        Assert.That(Day(vm, new DateTime(2026, 6, 10)).IsStart, Is.True);
        Assert.That(vm.Days.Any(d => d.IsBlackedOut), Is.False);
        vm.HoverDay(new DateTime(2026, 6, 20));
        Assert.That(vm.Days.Any(d => d.IsPreview), Is.False);
    }

    [Test]
    public void SingleMode_SecondSelect_ReplacesTheDate()
    {
        var vm = Single(new DateTime(2026, 6, 1), selected: new DateTime(2026, 6, 10));

        vm.SelectDay(new DateTime(2026, 6, 20));

        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 20)));
        Assert.That(vm.To, Is.Null);
    }

    [Test]
    public void MonthLabel_IsCultureFormattedMonthAndYear()
    {
        var vm = Range(new DateTime(2026, 6, 15));

        Assert.That(vm.MonthLabel, Is.EqualTo("June 2026"));
    }

    [Test]
    public void WeekdayHeaders_AreSevenNamesStartingAtCultureFirstDay()
    {
        var vm = Range(new DateTime(2026, 6, 15));

        Assert.That(vm.WeekdayHeaders, Is.EqualTo(new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }));
    }

    [Test]
    public void Grid_StartsOnTheCultureFirstDayOfWeek_BeforeTheMonth()
    {
        var vm = Range(new DateTime(2026, 6, 15));

        Assert.That(vm.Days[0].Date, Is.EqualTo(new DateTime(2026, 5, 31)));
    }

    [Test]
    public void StartAndEndDays_AreEndpoints_NotInRange()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10), to: new DateTime(2026, 6, 14));

        Assert.That(Day(vm, new DateTime(2026, 6, 10)).IsInRange, Is.False);
        Assert.That(Day(vm, new DateTime(2026, 6, 14)).IsInRange, Is.False);
        Assert.That(Day(vm, new DateTime(2026, 6, 10)).IsStart, Is.True);
    }

    [Test]
    public void StartDay_IsNeitherBlackedOutNorPreview_WhilePickingEnd()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10));
        vm.HoverDay(new DateTime(2026, 6, 14));

        var start = Day(vm, new DateTime(2026, 6, 10));
        Assert.That(start.IsBlackedOut, Is.False);
        Assert.That(start.IsPreview, Is.False);
        Assert.That(start.IsStart, Is.True);
    }

    [Test]
    public void SelectingTheStartDayAgain_WhilePickingEnd_MakesASingleDayRange()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10));

        vm.SelectDay(new DateTime(2026, 6, 10));

        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 10)));
        Assert.That(vm.To, Is.EqualTo(new DateTime(2026, 6, 10)));
    }

    [Test]
    public void SingleMode_SelectDay_RaisesSelectionChangedAndCommitted()
    {
        var vm = Single(new DateTime(2026, 6, 1));
        var selection = 0; var committed = 0;
        vm.SelectionChanged += (_, _) => selection++;
        vm.Committed += (_, _) => committed++;

        vm.SelectDay(new DateTime(2026, 6, 10));

        Assert.That(selection, Is.EqualTo(1));
        Assert.That(committed, Is.EqualTo(1));
    }

    [Test]
    public void RangeMode_FirstSelect_RaisesSelectionChanged_ButNotCommitted()
    {
        var vm = Range(new DateTime(2026, 6, 1));
        var selection = 0; var committed = 0;
        vm.SelectionChanged += (_, _) => selection++;
        vm.Committed += (_, _) => committed++;

        vm.SelectDay(new DateTime(2026, 6, 10));

        Assert.That(selection, Is.EqualTo(1));
        Assert.That(committed, Is.EqualTo(0));
    }

    [Test]
    public void RangeMode_SecondSelect_RaisesCommitted()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10));
        var committed = 0;
        vm.Committed += (_, _) => committed++;

        vm.SelectDay(new DateTime(2026, 6, 14));

        Assert.That(committed, Is.EqualTo(1));
    }

    [Test]
    public void Clear_RaisesSelectionChanged()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10), to: new DateTime(2026, 6, 14));
        var raised = 0;
        vm.SelectionChanged += (_, _) => raised++;

        vm.Clear();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Grid_HonoursAMondayFirstCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var vm = Range(new DateTime(2026, 6, 15));
            Assert.That(vm.Days[0].Date, Is.EqualTo(new DateTime(2026, 6, 1)));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void NextYearThenPrevYear_JumpsTwelveMonths_AndKeepsSelection()
    {
        var vm = Range(new DateTime(2026, 6, 1), from: new DateTime(2026, 6, 10));

        vm.NextYear();
        Assert.That(vm.Days.First(d => d.IsCurrentMonth).Date.Year, Is.EqualTo(2027));
        Assert.That(vm.MonthLabel, Is.EqualTo("June 2027"));

        vm.PrevYear();
        Assert.That(vm.Days.First(d => d.IsCurrentMonth).Date.Year, Is.EqualTo(2026));
        Assert.That(vm.From, Is.EqualTo(new DateTime(2026, 6, 10)));
    }

    [Test]
    public void DayCells_KeepTheSameInstancesAcrossRefresh_SoTheGridIsNotRebuilt()
    {
        var vm = Range(new DateTime(2026, 6, 1));
        var before = vm.Days.ToList();

        vm.SelectDay(new DateTime(2026, 6, 10));
        vm.HoverDay(new DateTime(2026, 6, 14));
        vm.NextMonth();

        Assert.That(vm.Days, Is.EqualTo(before).Using<DatePickerDayViewModel>(ReferenceEquals));
    }

    [Test]
    public void NextMonth_RaisesMonthLabelChange()
    {
        var vm = Range(new DateTime(2026, 6, 1));
        var changed = false;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.MonthLabel)) changed = true; };

        vm.NextMonth();

        Assert.That(changed, Is.True);
        Assert.That(vm.MonthLabel, Is.EqualTo("July 2026"));
    }
}
