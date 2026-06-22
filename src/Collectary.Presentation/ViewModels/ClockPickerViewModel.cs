using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Collectary.Presentation.ViewModels;

public enum ClockMode { Hour, Minute }

public partial class ClockPickerViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial int Hour { get; private set; }

    [ObservableProperty]
    public partial int Minute { get; private set; }

    [ObservableProperty]
    public partial ClockMode Mode { get; private set; }

    private int? _previewHour;
    private int? _previewMinute;

    public ObservableCollection<ClockNumberViewModel> Numbers { get; } = new();

    public ClockPickerViewModel(int hour, int minute)
    {
        Hour = hour;
        Minute = minute;
        Mode = ClockMode.Hour;
        RebuildNumbers();
    }

    public int DisplayHour => _previewHour ?? Hour;
    public int DisplayMinute => _previewMinute ?? Minute;

    public double SelectedAngle => Mode == ClockMode.Hour ? (DisplayHour % 12) * 30 : DisplayMinute * 6;
    public bool SelectedInner => Mode == ClockMode.Hour && (DisplayHour == 0 || DisplayHour > 12);

    public int HourAt(double angleDegrees, bool innerRing)
    {
        var h = ((int)Math.Round(angleDegrees / 30)) % 12;
        return innerRing ? (h == 0 ? 0 : h + 12) : (h == 0 ? 12 : h);
    }

    public int MinuteAt(double angleDegrees) => ((int)Math.Round(angleDegrees / 6)) % 60;

    public void Apply(double angleDegrees, bool innerRing)
    {
        _previewHour = null;
        _previewMinute = null;
        if (Mode == ClockMode.Hour)
        {
            Hour = HourAt(angleDegrees, innerRing);
            Mode = ClockMode.Minute;
        }
        else
        {
            Minute = MinuteAt(angleDegrees);
        }
        RebuildNumbers();
    }

    public void Preview(double angleDegrees, bool innerRing)
    {
        if (Mode == ClockMode.Hour) _previewHour = HourAt(angleDegrees, innerRing);
        else _previewMinute = MinuteAt(angleDegrees);
        RebuildNumbers();
    }

    public void ClearPreview()
    {
        if (_previewHour is null && _previewMinute is null) return;
        _previewHour = null;
        _previewMinute = null;
        RebuildNumbers();
    }

    public void EditHour()
    {
        ClearPreview();
        Mode = ClockMode.Hour;
        RebuildNumbers();
    }

    public void EditMinute()
    {
        ClearPreview();
        Mode = ClockMode.Minute;
        RebuildNumbers();
    }

    private void RebuildNumbers()
    {
        Numbers.Clear();
        if (Mode == ClockMode.Hour)
        {
            for (var h = 1; h <= 12; h++)
                Numbers.Add(new ClockNumberViewModel(h.ToString(), (h % 12) * 30, false, DisplayHour == h));
            Numbers.Add(new ClockNumberViewModel("00", 0, true, DisplayHour == 0));
            for (var h = 13; h <= 23; h++)
                Numbers.Add(new ClockNumberViewModel(h.ToString(), (h % 12) * 30, true, DisplayHour == h));
        }
        else
        {
            for (var m = 0; m < 60; m += 5)
                Numbers.Add(new ClockNumberViewModel(m.ToString("00"), m * 6, false, DisplayMinute == m));
        }
    }
}
