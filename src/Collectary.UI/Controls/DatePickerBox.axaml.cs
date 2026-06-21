using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Controls;

public partial class DatePickerBox : UserControl
{
    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<DatePickerBox, bool>(nameof(IsRange));

    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<DatePickerBox, DateTime?>(
            nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> FromProperty =
        AvaloniaProperty.Register<DatePickerBox, DateTime?>(
            nameof(From), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> ToProperty =
        AvaloniaProperty.Register<DatePickerBox, DateTime?>(
            nameof(To), defaultBindingMode: BindingMode.TwoWay);

    public bool IsRange { get => GetValue(IsRangeProperty); set => SetValue(IsRangeProperty, value); }
    public DateTime? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }
    public DateTime? From { get => GetValue(FromProperty); set => SetValue(FromProperty, value); }
    public DateTime? To { get => GetValue(ToProperty); set => SetValue(ToProperty, value); }

    private DatePickerCalendarViewModel? _calendar;
    private bool _pushing;

    public DatePickerBox()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!PART_Popup.IsOpen)
            BuildCalendar();
        PART_Popup.IsOpen = !PART_Popup.IsOpen;
    }

    private void BuildCalendar()
    {
        if (_calendar is not null)
        {
            _calendar.SelectionChanged -= OnCalendarSelectionChanged;
            _calendar.Committed -= OnCalendarCommitted;
        }

        var seedFrom = IsRange ? From : SelectedDate;
        var seedTo = IsRange ? To : null;
        var anchor = (seedFrom ?? seedTo ?? DateTime.Today).Date;
        _calendar = new DatePickerCalendarViewModel(IsRange, anchor, seedFrom, seedTo);
        _calendar.SelectionChanged += OnCalendarSelectionChanged;
        _calendar.Committed += OnCalendarCommitted;
        PART_PopupRoot.DataContext = _calendar;
    }

    private void OnCalendarSelectionChanged(object? sender, EventArgs e)
    {
        if (_calendar is null) return;
        _pushing = true;
        try
        {
            if (IsRange)
            {
                From = _calendar.From;
                To = _calendar.To;
            }
            else
            {
                SelectedDate = _calendar.From;
            }
        }
        finally
        {
            _pushing = false;
        }
        UpdateDisplay();
    }

    private void OnCalendarCommitted(object? sender, EventArgs e) => PART_Popup.IsOpen = false;

    private void OnDayClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: DatePickerDayViewModel day })
            _calendar?.SelectDay(day.Date);
    }

    private void OnDayPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: DatePickerDayViewModel day })
            _calendar?.HoverDay(day.Date);
    }

    private void OnGridPointerExited(object? sender, PointerEventArgs e) => _calendar?.ClearHover();

    private void OnPrevMonth(object? sender, RoutedEventArgs e) => _calendar?.PrevMonth();

    private void OnNextMonth(object? sender, RoutedEventArgs e) => _calendar?.NextMonth();

    private void OnPrevYear(object? sender, RoutedEventArgs e) => _calendar?.PrevYear();

    private void OnNextYear(object? sender, RoutedEventArgs e) => _calendar?.NextYear();

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _calendar?.Clear();
        PART_Popup.IsOpen = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_pushing) return;
        if (change.Property == SelectedDateProperty || change.Property == FromProperty
            || change.Property == ToProperty || change.Property == IsRangeProperty)
            UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var culture = CultureInfo.CurrentCulture;
        var pattern = culture.DateTimeFormat.ShortDatePattern;
        if (IsRange)
        {
            if (From.HasValue || To.HasValue)
            {
                PART_Display.Text = new DateRangeTextFormatter().Format(From, To, culture);
                PART_Display.Opacity = 1.0;
            }
            else
            {
                PART_Display.Text = $"{pattern} → {pattern}";
                PART_Display.Opacity = 0.5;
            }
        }
        else
        {
            PART_Display.Text = SelectedDate?.ToString("d", culture) ?? pattern;
            PART_Display.Opacity = SelectedDate.HasValue ? 1.0 : 0.5;
        }
    }
}
