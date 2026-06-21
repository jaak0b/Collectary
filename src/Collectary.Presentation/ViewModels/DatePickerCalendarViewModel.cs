using System.Collections.ObjectModel;
using System.Globalization;

namespace Collectary.Presentation.ViewModels;

public class DatePickerCalendarViewModel : ViewModelBase
{
    private const int GridCells = 42;

    private int _year;
    private int _month;
    private DateTime? _hover;

    public bool RangeMode { get; }
    public DateTime? From { get; private set; }
    public DateTime? To { get; private set; }

    public ObservableCollection<DatePickerDayViewModel> Days { get; } = new();
    public IReadOnlyList<string> WeekdayHeaders { get; private set; } = Array.Empty<string>();

    public string MonthLabel => new DateTime(_year, _month, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    public event EventHandler? SelectionChanged;
    public event EventHandler? Committed;

    public DatePickerCalendarViewModel(bool rangeMode, DateTime visibleMonth, DateTime? from = null, DateTime? to = null)
    {
        RangeMode = rangeMode;
        _year = visibleMonth.Year;
        _month = visibleMonth.Month;
        From = from?.Date;
        To = to?.Date;
        BuildWeekdayHeaders();
        for (var i = 0; i < GridCells; i++)
            Days.Add(new DatePickerDayViewModel());
        Refresh();
    }

    private bool IsPickingEnd => RangeMode && From.HasValue && !To.HasValue;

    public void SelectDay(DateTime date)
    {
        var day = date.Date;
        if (!RangeMode || !From.HasValue || To.HasValue || day < From.Value.Date)
        {
            From = day;
            To = null;
        }
        else
        {
            To = day;
        }

        _hover = null;
        Refresh();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        if (!RangeMode || To.HasValue)
            Committed?.Invoke(this, EventArgs.Empty);
    }

    public void HoverDay(DateTime date)
    {
        if (!IsPickingEnd) return;
        _hover = date.Date;
        Refresh();
    }

    public void ClearHover()
    {
        if (_hover is null) return;
        _hover = null;
        Refresh();
    }

    public void PrevMonth() => ShiftMonth(-1);

    public void NextMonth() => ShiftMonth(1);

    public void PrevYear() => ShiftMonth(-12);

    public void NextYear() => ShiftMonth(12);

    public void Clear()
    {
        From = null;
        To = null;
        _hover = null;
        Refresh();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShiftMonth(int delta)
    {
        var anchor = new DateTime(_year, _month, 1).AddMonths(delta);
        _year = anchor.Year;
        _month = anchor.Month;
        Refresh();
        OnPropertyChanged(nameof(MonthLabel));
    }

    private void BuildWeekdayHeaders()
    {
        var culture = CultureInfo.CurrentCulture;
        var firstDay = (int)culture.DateTimeFormat.FirstDayOfWeek;
        var names = culture.DateTimeFormat.AbbreviatedDayNames;
        WeekdayHeaders = Enumerable.Range(0, 7).Select(i => names[(firstDay + i) % 7]).ToList();
    }

    private void Refresh()
    {
        var culture = CultureInfo.CurrentCulture;
        var firstOfMonth = new DateTime(_year, _month, 1);
        var firstDay = (int)culture.DateTimeFormat.FirstDayOfWeek;
        var offset = ((int)firstOfMonth.DayOfWeek - firstDay + 7) % 7;
        var gridStart = firstOfMonth.AddDays(-offset);

        for (var i = 0; i < GridCells; i++)
            UpdateCell(Days[i], gridStart.AddDays(i));
    }

    private void UpdateCell(DatePickerDayViewModel cell, DateTime date)
    {
        var day = date.Date;
        cell.Date = date;
        cell.Label = date.Day.ToString();
        cell.IsCurrentMonth = date.Month == _month && date.Year == _year;
        cell.IsStart = From.HasValue && day == From.Value.Date;
        cell.IsEnd = To.HasValue && day == To.Value.Date;
        cell.IsInRange = From.HasValue && To.HasValue && day > From.Value.Date && day < To.Value.Date;
        cell.IsPreview = IsPickingEnd && _hover.HasValue
            && _hover.Value.Date >= From!.Value.Date
            && day > From.Value.Date && day <= _hover.Value.Date;
        cell.IsBlackedOut = IsPickingEnd && day < From!.Value.Date;
    }
}
