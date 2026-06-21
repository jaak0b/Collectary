using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class RatingFieldEditorView : UserControl
{
    public RatingFieldEditorView()
    {
        InitializeComponent();
    }

    private RatingFieldEditorViewModel? ViewModel => DataContext as RatingFieldEditorViewModel;

    private void OnStarClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: RatingStarViewModel star })
            ViewModel?.SetRating(star.Position);
    }

    private void OnStarPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: RatingStarViewModel star })
            ViewModel?.PreviewRating(star.Position);
    }

    private void OnRowPointerExited(object? sender, PointerEventArgs e) => ViewModel?.ClearPreview();
}
