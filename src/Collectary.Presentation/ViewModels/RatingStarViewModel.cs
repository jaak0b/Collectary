using CommunityToolkit.Mvvm.ComponentModel;

namespace Collectary.Presentation.ViewModels;

/// <summary>One star slot in a rating row; <see cref="IsLit"/> drives filled vs. outline rendering.</summary>
public partial class RatingStarViewModel : ViewModelBase
{
    public int Position { get; }

    [ObservableProperty]
    public partial bool IsLit { get; set; }

    public RatingStarViewModel(int position, bool isLit)
    {
        Position = position;
        IsLit = isLit;
    }
}
