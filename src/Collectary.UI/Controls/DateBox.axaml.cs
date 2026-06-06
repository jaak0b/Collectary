using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Collectary.UI.Controls;

public partial class DateBox : UserControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<DateBox, DateTime?>(
            nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateBox()
    {
        InitializeComponent();
        PART_Calendar.SelectedDatesChanged += OnCalendarSelectionChanged;
        UpdateDisplay();
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e) =>
        PART_Popup.IsOpen = !PART_Popup.IsOpen;

    private void OnCalendarSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedDate = PART_Calendar.SelectedDate;
        PART_Popup.IsOpen = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != SelectedDateProperty) return;
        if (PART_Calendar.SelectedDate != SelectedDate)
            PART_Calendar.SelectedDate = SelectedDate;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var culture = CultureInfo.CurrentCulture;
        PART_Display.Text = SelectedDate?.ToString("d", culture) ?? culture.DateTimeFormat.ShortDatePattern;
        PART_Display.Opacity = SelectedDate.HasValue ? 1.0 : 0.5;
    }
}
