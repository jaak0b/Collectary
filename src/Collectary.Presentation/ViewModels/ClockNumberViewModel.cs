namespace Collectary.Presentation.ViewModels;

public class ClockNumberViewModel
{
    public string Label { get; }
    public double Angle { get; }
    public bool IsInnerRing { get; }
    public bool IsSelected { get; }

    public ClockNumberViewModel(string label, double angle, bool isInnerRing, bool isSelected)
    {
        Label = label;
        Angle = angle;
        IsInnerRing = isInnerRing;
        IsSelected = isSelected;
    }
}
