using CommunityToolkit.Mvvm.ComponentModel;

namespace Collectary.Presentation.ViewModels;

public partial class DatePickerDayViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial DateTime Date { get; set; }

    [ObservableProperty]
    public partial string? Label { get; set; }

    [ObservableProperty]
    public partial bool IsCurrentMonth { get; set; }

    [ObservableProperty]
    public partial bool IsStart { get; set; }

    [ObservableProperty]
    public partial bool IsEnd { get; set; }

    [ObservableProperty]
    public partial bool IsInRange { get; set; }

    [ObservableProperty]
    public partial bool IsPreview { get; set; }

    [ObservableProperty]
    public partial bool IsBlackedOut { get; set; }

    public bool IsSelectable => !IsBlackedOut;

    partial void OnIsBlackedOutChanged(bool value) => OnPropertyChanged(nameof(IsSelectable));
}
