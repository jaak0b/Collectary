using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Controls;

public partial class TimeClockBox : UserControl
{
    private const double Center = 120;
    private const double OuterRadius = 96;
    private const double InnerRadius = 60;

    public static readonly StyledProperty<TimeSpan?> TimeProperty =
        AvaloniaProperty.Register<TimeClockBox, TimeSpan?>(
            nameof(Time), defaultBindingMode: BindingMode.TwoWay);

    public TimeSpan? Time
    {
        get => GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    private ClockPickerViewModel? _clock;
    private bool _committing;

    public TimeClockBox()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!PART_Popup.IsOpen)
        {
            var start = Time ?? new TimeSpan(12, 0, 0);
            _clock = new ClockPickerViewModel(start.Hours, start.Minutes);
            RenderFace();
        }
        PART_Popup.IsOpen = !PART_Popup.IsOpen;
    }

    private void OnEditHour(object? sender, PointerPressedEventArgs e)
    {
        _clock?.EditHour();
        RenderFace();
    }

    private void OnEditMinute(object? sender, PointerPressedEventArgs e)
    {
        _clock?.EditMinute();
        RenderFace();
    }

    private void OnFacePressed(object? sender, PointerPressedEventArgs e)
    {
        if (_clock is null) return;
        var hit = AngleAndRing(e.GetPosition(PART_Face));
        var wasMinute = _clock.Mode == ClockMode.Minute;
        _clock.Apply(hit.Angle, hit.Inner);
        CommitTime();
        RenderFace();
        if (wasMinute) PART_Popup.IsOpen = false;
    }

    private void OnFaceHover(object? sender, PointerEventArgs e)
    {
        if (_clock is null) return;
        var hit = AngleAndRing(e.GetPosition(PART_Face));
        _clock.Preview(hit.Angle, hit.Inner);
        RenderFace();
    }

    private void OnFaceExit(object? sender, PointerEventArgs e)
    {
        _clock?.ClearPreview();
        RenderFace();
    }

    private (double Angle, bool Inner) AngleAndRing(Point p)
    {
        var dx = p.X - Center;
        var dy = p.Y - Center;
        var angle = Math.Atan2(dx, -dy) * 180 / Math.PI;
        if (angle < 0) angle += 360;
        var inner = _clock!.Mode == ClockMode.Hour && Math.Sqrt(dx * dx + dy * dy) < (InnerRadius + OuterRadius) / 2;
        return (angle, inner);
    }

    private void CommitTime()
    {
        if (_clock is null) return;
        _committing = true;
        try { Time = new TimeSpan(_clock.Hour, _clock.Minute, 0); }
        finally { _committing = false; }
        UpdateDisplay();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TimeProperty && !_committing) UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        PART_Display.Text = Time is { } t ? $"{t.Hours:00}:{t.Minutes:00}" : "--:--";
        PART_Display.Opacity = Time is null ? 0.5 : 1.0;
    }

    private void RenderFace()
    {
        if (_clock is null) return;
        PART_Face.Children.Clear();

        var accent = this.FindResource("ClockAccentBrush") as IBrush ?? Brushes.SteelBlue;
        var primary = this.FindResource("TextPrimaryBrush") as IBrush ?? Brushes.Black;
        var ring = this.FindResource("BorderBrush") as IBrush ?? Brushes.Gray;

        var hourMode = _clock.Mode == ClockMode.Hour;
        AddGuideRing(OuterRadius, ring);
        if (hourMode) AddGuideRing(InnerRadius, ring);

        var end = Polar(_clock.SelectedAngle, _clock.SelectedInner ? InnerRadius : OuterRadius);
        PART_Face.Children.Add(new Line { StartPoint = new Point(Center, Center), EndPoint = end, Stroke = accent, StrokeThickness = 2 });
        AddDot(Center, Center, 4, accent);
        AddDot(end.X, end.Y, 18, accent);

        foreach (var number in _clock.Numbers)
        {
            var pos = Polar(number.Angle, number.IsInnerRing ? InnerRadius : OuterRadius);
            var label = new TextBlock
            {
                Text = number.Label,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = number.IsSelected ? Brushes.White : primary,
            };
            var box = new Border { Width = 30, Height = 30, Child = label, IsHitTestVisible = false };
            Canvas.SetLeft(box, pos.X - 15);
            Canvas.SetTop(box, pos.Y - 15);
            PART_Face.Children.Add(box);
        }

        PART_HourLabel.Text = $"{_clock.DisplayHour:00}";
        PART_MinuteLabel.Text = $"{_clock.DisplayMinute:00}";
        PART_HourLabel.Foreground = hourMode ? accent : primary;
        PART_MinuteLabel.Foreground = hourMode ? primary : accent;
        PART_StepLabel.Text = LocalizationService.Instance[hourMode ? "Clock_SelectHour" : "Clock_SelectMinute"];
    }

    private void AddGuideRing(double radius, IBrush stroke)
    {
        var circle = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Stroke = stroke,
            StrokeThickness = 1,
            Opacity = 0.4,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(circle, Center - radius);
        Canvas.SetTop(circle, Center - radius);
        PART_Face.Children.Add(circle);
    }

    private void AddDot(double x, double y, double radius, IBrush fill)
    {
        var dot = new Ellipse { Width = radius * 2, Height = radius * 2, Fill = fill, IsHitTestVisible = false };
        Canvas.SetLeft(dot, x - radius);
        Canvas.SetTop(dot, y - radius);
        PART_Face.Children.Add(dot);
    }

    private Point Polar(double angleDegrees, double radius)
    {
        var t = angleDegrees * Math.PI / 180;
        return new Point(Center + radius * Math.Sin(t), Center - radius * Math.Cos(t));
    }
}
